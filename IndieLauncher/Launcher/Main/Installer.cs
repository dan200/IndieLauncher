using System;
using System.IO;
using System.Linq;
using Ionic.Zip;
using System.Net;
using System.Reflection;
using Dan200.Launcher.Util;
using Dan200.Launcher.RSS;

namespace Dan200.Launcher.Main
{
    public static class Installer
    {
        public static string GetBasePath()
        {
            string basePath;
            if( Program.Platform == Platform.OSX )
            {
                basePath = Path.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.Personal ),
                    "Library/Application Support"
                );
            }
            else
            {
                basePath = Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
            }
            basePath = Path.Combine( basePath, "IndieLauncher" );
            return basePath;
        }

        public static string GetBasePath( string gameTitle )
        {
            string basePath = GetBasePath();
            basePath = Path.Combine( basePath, "Games" );
            basePath = Path.Combine( basePath, gameTitle );
            return basePath;
        }

        public static string GetDownloadPath( string gameTitle, string gameVersion )
        {
            var downloadPath = GetBasePath( gameTitle );
            downloadPath = Path.Combine( downloadPath, "Downloads" );
            downloadPath = Path.Combine( downloadPath, gameVersion + ".zip" );
            return downloadPath;
        }

        public static string GetInstallPath( string gameTitle, string gameVersion )
        {
            var installPath = GetBasePath( gameTitle );
            installPath = Path.Combine( installPath, "Versions" );
            installPath = Path.Combine( installPath, gameVersion );
            return installPath;
        }

        public static bool IsGameDownloaded( string gameTitle, string gameVersion )
        {
            var downloadPath = GetDownloadPath( gameTitle, gameVersion );
            return File.Exists( downloadPath );
        }

        public static string GetLatestInstalledVersion( string gameTitle )
        {
            var versionPath = Installer.GetBasePath( gameTitle );
            versionPath = Path.Combine( versionPath, "Versions" );
            versionPath = Path.Combine( versionPath, "Latest.txt" );
            if( File.Exists( versionPath ) )
            {
                string gameVersion = File.ReadAllText( versionPath ).Trim();
                if( IsGameInstalled( gameTitle, gameVersion ) )
                {
                    return gameVersion;
                }
            }
            return null;
        }

        public static string RecordLatestInstalledVersion( string gameTitle, string gameVersion, bool overwrite )
        {
            var versionPath = Installer.GetBasePath( gameTitle );
            versionPath = Path.Combine( versionPath, "Versions" );
            versionPath = Path.Combine( versionPath, "Latest.txt" );
            if( overwrite || !File.Exists( versionPath ) )
            {
                File.WriteAllText( versionPath, gameVersion );
            }
            return null;
        }

        public static bool GetLatestVersionInfo( RSSFile rssFile, string gameTitle, out string o_gameVersion, out string o_gameDescription, out string o_versionDownloadURL, out string o_versionDescription, out bool o_versionIsNewest )
        {
            // Find a matching title
            foreach( var channel in rssFile.Channels )
            {
                if( channel.Title == gameTitle )
                {
                    // Find a matching version
                    for( int i=0; i<channel.Entries.Count; ++i )
                    {
                        var entry = channel.Entries[ i ];
                        if( entry.Title != null )
                        {
                            o_gameVersion = entry.Title;
                            o_gameDescription = (channel.Description != null) ? channel.Description : channel.Title;
                            o_versionDownloadURL = entry.Link;
                            o_versionDescription = entry.Description;
                            o_versionIsNewest = (i == 0);
                            return true;
                        }
                    }
                }
            }
            o_gameVersion = null;
            o_gameDescription = null;
            o_versionDownloadURL = null;
            o_versionDescription = null;
            o_versionIsNewest = false;
            return false;
        }

        public static bool GetSpecificVersionInfo( RSSFile rssFile, string gameTitle, string gameVersion, out string o_gameDescription, out string o_versionDownloadURL, out string o_versionDescription, out bool o_versionIsNewest )
        {
            // Find a matching title
            foreach( var channel in rssFile.Channels )
            {
                if( channel.Title == gameTitle )
                {
                    // Find a matching version
                    for( int i=0; i<channel.Entries.Count; ++i )
                    {
                        var entry = channel.Entries[ i ];
                        if( entry.Title == gameVersion )
                        {
                            o_gameDescription = (channel.Description != null) ? channel.Description : channel.Title;
                            o_versionDownloadURL = entry.Link;
                            o_versionDescription = entry.Description;
                            o_versionIsNewest = (i == 0);
                            return true;
                        }
                    }
                }
            }
            o_gameDescription = null;
            o_versionDownloadURL = null;
            o_versionDescription = null;
            o_versionIsNewest = false;
            return false;
        }

        public static bool GetEmbeddedGameInfo( out string o_gameTitle, out string o_gameVersion, out string o_gameURL )
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var stream = assembly.GetManifestResourceStream( "EmbeddedGame.txt" );
                if( stream != null )
                {
                    var kvp = new KeyValuePairs();
                    kvp.Load( stream );
                    if( kvp.ContainsKey( "game" ) )
                    {
                        o_gameTitle = kvp.GetString( "game" );
                        o_gameVersion = kvp.GetString( "version" );
                        o_gameURL = kvp.GetString( "url" );
                        return true;
                    }
                }
                o_gameTitle = default( string );
                o_gameVersion = default( string );
                o_gameURL = default( string );
                return false;
            }
            catch( Exception )
            {
                o_gameTitle = default( string );
                o_gameVersion = default( string );
                o_gameURL = default( string );
                return false;
            }
        }

        public static string GetEmbeddedGameVersion( string gameTitle )
        {
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( GetEmbeddedGameInfo( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                if( embeddedGameTitle == gameTitle )
                {
                    return embeddedGameVersion;
                }
            }
            return null;
        }

        public static bool ExtractEmbeddedGame( ProgressDelegate listener, ICancellable cancelObject )
        {
            string gameTitle, gameVersion, gameURL;
            if( GetEmbeddedGameInfo( out gameTitle, out gameVersion, out gameURL ) && gameVersion != null )
            {
                var downloadPath = GetDownloadPath( gameTitle, gameVersion );
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var stream = assembly.GetManifestResourceStream( "EmbeddedGame.zip" );
                    if( stream != null )
                    {
                        using( stream )
                        {
                            try
                            {
                                using( var progressStream = new ProgressStream( stream, listener, cancelObject ) )
                                {
                                    // Delete old download
                                    if( File.Exists( downloadPath ) )
                                    {
                                        File.Delete( downloadPath );
                                    }

                                    // Create new download
                                    try
                                    {
                                        Directory.CreateDirectory( Path.GetDirectoryName( downloadPath ) );
                                        using( var output = File.OpenWrite( downloadPath ) )
                                        {
                                            try
                                            {
                                                progressStream.CopyTo( output );
                                            }
                                            finally
                                            {
                                                output.Close();
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        progressStream.Close();
                                    }
                                }
                            }
                            finally
                            {
                                stream.Close();
                            }
                        }
                        return true;
                    }
                    return false;
                }
                catch( IOException )
                {
                    if( File.Exists( downloadPath ) )
                    {
                        File.Delete( downloadPath );
                    }
                    return false;
                }
            }
            return false;
        }

        public static bool DownloadGame( string gameTitle, string gameVersion, string url, ProgressDelegate listener, ICancellable cancelObject )
        {
            if( url == null )
            {
                return false;
            }

            var downloadPath = GetDownloadPath( gameTitle, gameVersion );
            try
            {
                var request = HttpWebRequest.Create( url );
                request.Timeout = 10000;
                using( var response = request.GetResponse() )
                {
                    using( var stream = new ProgressStream( response.GetResponseStream(), listener, cancelObject ) )
                    {
                        try
                        {
                            // Delete old download
                            if( File.Exists( downloadPath ) )
                            {
                                File.Delete( downloadPath );
                            }

                            // Create new download
                            Directory.CreateDirectory( Path.GetDirectoryName( downloadPath ) );
                            using( var output = File.OpenWrite( downloadPath ) )
                            {
                                try
                                {
                                    stream.CopyTo( output );
                                }
                                finally
                                {
                                    output.Close();
                                }
                            }
                        }
                        finally
                        {
                            stream.Close();
                        }
                    }
                }
                return true;
            }
            catch( IOException )
            {
                if( File.Exists( downloadPath ) )
                {
                    File.Delete( downloadPath );
                }
                return false;
            }
            catch( WebException )
            {
                if( File.Exists( downloadPath ) )
                {
                    File.Delete( downloadPath );
                }
                return false;
            }
        }

        public static bool IsGameInstalled( string gameTitle, string gameVersion )
        {
            var installPath = GetInstallPath( gameTitle, gameVersion );
            return Directory.Exists( installPath );
        }

        public static string[] GetInstalledGames()
        {
            var gamesPath = GetBasePath();
            gamesPath = Path.Combine( gamesPath, "Games" );
            return Directory.GetDirectories( gamesPath ).Select( p => Path.GetFileName(p) ).ToArray();
        }

        public static bool InstallGame( string gameTitle, string gameVersion, ProgressDelegate listener, ICancellable cancelObject )
        {
            var downloadPath = GetDownloadPath( gameTitle, gameVersion );
            var installPath = GetInstallPath( gameTitle, gameVersion );
            if( File.Exists( downloadPath ) )
            {
                try
                {
                    using( var zipFile = new ZipFile( downloadPath ) )
                    {
                        // Delete old install
                        if( Directory.Exists( installPath ) )
                        {
                            Directory.Delete( installPath, true );
                        }
                        Directory.CreateDirectory( installPath );

                        // Extract new install
                        int totalFiles = zipFile.Entries.Count;
                        int filesInstalled = 0;
                        int lastProgress = 0;
                        listener.Invoke( 0 );
                        foreach( var entry in zipFile.Entries )
                        {
                            // Extract the file
                            var entryInstallPath = Path.Combine( installPath, entry.FileName );
                            if( entry.IsDirectory )
                            {
                                Directory.CreateDirectory( entryInstallPath );
                            }
                            else
                            {
                                Directory.CreateDirectory( Path.GetDirectoryName( entryInstallPath ) );
                                using( var file = File.OpenWrite( entryInstallPath ) )
                                {
                                    try
                                    {
                                        using( var reader = new ProgressStream( entry.OpenReader(), delegate {
                                            // TODO: Emit progress during installation of large individual files?
                                        }, cancelObject ) )
                                        {
                                            try
                                            {
                                                reader.CopyTo( file );
                                            }
                                            finally
                                            {
                                                reader.Close();
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        file.Close();
                                    }
                                }
                            }

                            // Check for cancellation
                            if( cancelObject.Cancelled )
                            {
                                throw new IOCancelledException();
                            }

                            // Notify the progress listener
                            filesInstalled++;
                            int progress = (filesInstalled * 100) / totalFiles;
                            if( progress != lastProgress )
                            {
                                listener.Invoke( progress );
                                lastProgress = progress;
                            }
                        }
                    }
                    return true;
                }
                catch( IOException )
                {
                    if( Directory.Exists( installPath ) )
                    {
                        Directory.Delete( installPath, true );
                    }
                    return false;
                }
                catch( ZipException )
                {
                    if( Directory.Exists( installPath ) )
                    {
                        Directory.Delete( installPath, true );
                    }
                    return false;
                }
            }
            return false;
        }
    }
}


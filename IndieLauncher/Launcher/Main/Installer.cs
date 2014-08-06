using System;
using System.IO;
using Ionic.Zip;
using System.Net;
using System.Reflection;
using Dan200.Launcher.Util;
using Dan200.Launcher.RSS;

namespace Dan200.Launcher.Main
{
    public static class Installer
    {
        public static string GetBasePath( string gameTitle )
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
            var gamePath = Installer.GetBasePath( gameTitle );
            if( File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                string gameVersion = File.ReadAllText( Path.Combine( gamePath, "LatestVersion.txt" ) ).Trim();
                if( IsGameInstalled( gameTitle, gameVersion ) )
                {
                    return gameVersion;
                }
            }
            return null;
        }

        public static string RecordLatestInstalledVersion( string gameTitle, string gameVersion, bool overwrite )
        {
            var gamePath = Installer.GetBasePath( gameTitle );
            if( overwrite || !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
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

        public static bool GetEmbeddedGame( out string o_gameTitle, out string o_gameVersion, out string o_gameURL )
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
            if( GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                if( embeddedGameTitle == gameTitle )
                {
                    return embeddedGameVersion;
                }
            }
            return null;
        }

        public static bool ExtractEmbeddedGame( ProgressDelegate listener )
        {
            string gameTitle, gameVersion, gameURL;
            if( GetEmbeddedGame( out gameTitle, out gameVersion, out gameURL ) && gameVersion != null )
            {
                var downloadPath = GetDownloadPath( gameTitle, gameVersion );
                try
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var stream = assembly.GetManifestResourceStream( "EmbeddedGame.zip" );
                    if( stream != null )
                    {
                        // Delete old download
                        if( File.Exists( downloadPath ) )
                        {
                            File.Delete( downloadPath );
                        }

                        // Create new download
                        using( var progressStream = new ProgressStream( stream, listener ) )
                        {
                            Directory.CreateDirectory( Path.GetDirectoryName( downloadPath ) );
                            using( var output = File.OpenWrite( downloadPath ) )
                            {
                                progressStream.CopyTo( output );
                                output.Close();
                            }
                            progressStream.Close();
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

        public static bool DownloadGame( string gameTitle, string gameVersion, string url, ProgressDelegate listener )
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
                    using( var stream = new ProgressStream( response.GetResponseStream(), listener ) )
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
                            stream.CopyTo( output );
                            output.Close();
                        }
                        stream.Close();
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

        public static bool InstallGame( string gameTitle, string gameVersion )
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
                        foreach( var entry in zipFile.Entries )
                        {
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
                                    using( var reader = entry.OpenReader() )
                                    {
                                        reader.CopyTo( file );
                                        reader.Close();
                                    }
                                    file.Close();
                                }
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


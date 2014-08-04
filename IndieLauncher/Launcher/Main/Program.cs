using System;
using Dan200.Launcher.RSS;
using System.Net;
using System.IO;
using System.Diagnostics;
using Ionic.Zip;
using System.Runtime.InteropServices;
using Dan200.Launcher.Util;
using System.Reflection;

namespace Dan200.Launcher.Main
{
	public class Program
	{
        [DllImport( "libc" )]
        static extern int uname( IntPtr buf );

        public static Platform Platform
        {
            get;
            private set;
        }

        public static ProgramArguments Arguments
        {
            get;
            private set;
        }

        private static Platform DeterminePlatform()
        {        
            switch( Environment.OSVersion.Platform )
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                {
                    return Platform.Windows;
                }
                case PlatformID.MacOSX:
                {
                    return Platform.OSX;
                }
                case PlatformID.Unix:
                {
                    IntPtr buffer = IntPtr.Zero;
                    try
                    { 
                        buffer = Marshal.AllocHGlobal( 8192 ); 
                        if( uname( buffer ) == 0 )
                        { 
                            string os = Marshal.PtrToStringAnsi( buffer );
                            if( os == "Darwin" )
                            {
                                return Platform.OSX;
                            }
                            else if( os == "Linux" )
                            {
                                return Platform.Linux;
                            }
                        }
                        return Platform.Unknown;
                    }
                    catch( Exception )
                    {
                        return Platform.Unknown;
                    }
                    finally
                    {
                        if( buffer != IntPtr.Zero )
                        {
                            Marshal.FreeHGlobal( buffer );
                        }
                    }
                }
                default:
                {
                    return Platform.Unknown;
                }
            }
        }

        private static string GetBaseStorageDirectory()
        {
            if( Platform == Platform.OSX )
            {
                return Path.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.Personal ),
                    "Library/Application Support"
                );
            }
            else
            {
                return Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData );
            }
        }

        private static string GetStorageDirectory()
        {
            return Path.Combine( GetBaseStorageDirectory(), "IndieLauncher" );
        }

        private static RSSFile DownloadRSS( string url )
        {
            try
            {
                var request = HttpWebRequest.Create( url );
                request.Timeout = 15000;
                using( var response = request.GetResponse() )
                {
                    using( var stream = response.GetResponseStream() )
                    {
                        return new RSSFile( stream );
                    }
                }
            }
            catch( IOException )
            {
                return null;
            }
            catch( WebException )
            {
                return null;
            }
        }

        private static bool DownloadFile( string url, string path )
        {
            try
            {
                var request = HttpWebRequest.Create( url );
                request.Timeout = 15000;
                using( var response = request.GetResponse() )
                {
                    using( var stream = response.GetResponseStream() )
                    {
                        Directory.CreateDirectory( Path.GetDirectoryName( path ) );
                        using( var output = File.OpenWrite( path ) )
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
                File.Delete( path );
                return false;
            }
            catch( WebException )
            {
                File.Delete( path );
                return false;
            }
        }

        private static bool InstallFile( string downloadPath, string installPath )
        {
            try
            {
                using( var zipFile = new ZipFile( downloadPath ) )
                {
                    Directory.CreateDirectory( installPath );
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

        private static RSSEntry GetMatchingEntry( RSSFile file, ref string io_game, ref string io_version, ref bool o_isLatest )
        {
            if( file != null )
            {
                foreach( var channel in file.Channels )
                {
                    if( channel.Title != null )
                    {
                        if( io_game == null || channel.Title == io_game )
                        {
                            io_game = channel.Title;
                            for( int i=0; i<channel.Entries.Count; ++i )
                            {
                                var entry = channel.Entries[ i ];
                                if( entry.Title != null )
                                {
                                    if( io_version == null || entry.Title == io_version )
                                    {
                                        io_version = entry.Title;
                                        o_isLatest = (i == 0);
                                        return entry;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            return null;
        }

        private static bool DownloadAndInstall( string downloadURL, string gameTitle, string gameVersion, bool isLatest )
        {
            // Build the path
            string gamePath = GetStorageDirectory();
            gamePath = Path.Combine( gamePath, "Games" );
            gamePath = Path.Combine( gamePath, gameTitle );

            // Download
            string downloadPath = gamePath;
            downloadPath = Path.Combine( downloadPath, "Downloads" );
            downloadPath = Path.Combine( downloadPath, gameVersion + ".zip" );
            if( !File.Exists( downloadPath ) )
            {
                Console.Write( "Downloading update... " );
                if( DownloadFile( downloadURL, downloadPath ) )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }
            else
            {
                Console.WriteLine( "Update already downloaded." );
            }

            // Install
            string installPath = gamePath;
            installPath = Path.Combine( installPath, "Versions" );
            installPath = Path.Combine( installPath, gameVersion );
            if( !Directory.Exists( installPath ) )
            {
                Console.Write( "Installing update... " );
                if( InstallFile( downloadPath, installPath ) )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }
            else
            {
                Console.WriteLine( "Update already installed." );
            }

            // Record that this file is the latest
            if( isLatest || !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
            }
            return true;
        }

        private static Stream OpenEmbeddedResource( string name )
        {
            var assembly = Assembly.GetAssembly( typeof(Program) );
            return assembly.GetManifestResourceStream( name );
        }

        private static bool GetEmbeddedGameData( ref string o_gameURL, ref string o_gameTitle, ref string o_gameVersion )
        {
            using( var stream = OpenEmbeddedResource( "EmbeddedGame.txt" ) )
            {
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
            }
            return false;
        }

        private static bool ExtractEmbeddedGame( string path )
        {
            try
            {
                using( var stream = OpenEmbeddedResource( "EmbeddedGame.zip" ) )
                {
                    if( stream != null )
                    {
                        Directory.CreateDirectory( Path.GetDirectoryName( path ) );
                        using( var output = File.OpenWrite( path ) )
                        {
                            stream.CopyTo( output );
                            output.Close();
                        }
                        stream.Close();
                        return true;
                    }
                }
                return false;
            }
            catch( IOException )
            {
                File.Delete( path );
                return false;
            }
        }

        private static bool ExtractAndInstallEmbeddedGame( string gameTitle, string gameVersion )
        {
            // Build the path
            string gamePath = GetStorageDirectory();
            gamePath = Path.Combine( gamePath, "Games" );
            gamePath = Path.Combine( gamePath, gameTitle );

            // Extract the embedded game
            string downloadPath = gamePath;
            downloadPath = Path.Combine( downloadPath, "Downloads" );
            downloadPath = Path.Combine( downloadPath, gameVersion + ".zip" );
            if( !File.Exists( downloadPath ) )
            {                   
                Console.Write( "Extracting embedded game... " );
                if( ExtractEmbeddedGame( downloadPath ) )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }

            // Install the embedded game
            string installPath = gamePath;
            installPath = Path.Combine( installPath, "Versions" );
            installPath = Path.Combine( installPath, gameVersion );
            if( !Directory.Exists( installPath ) )
            {
                Console.Write( "Installing embedded game... " );
                if( InstallFile( downloadPath, installPath ) )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }

            // Record that this file is the latest
            if( !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
            }
            return true;
        }

        private static void SetupEmbeddedAssemblies()
        {
            EmbeddedAssembly.Load( "Ionic.Zip.dll" );
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args )
            {
                return EmbeddedAssembly.Get( args.Name );
            };
        }

		public static void Main( string[] args )
		{
            // Init
            SetupEmbeddedAssemblies();
            Platform = DeterminePlatform();
            Arguments = new ProgramArguments( args );

            // Get the search params
            string gameURL = Arguments.GetString( "url" );
            string gameTitle = Arguments.GetString( "game" );
            string gameVersion = Arguments.GetString( "version" );
            string embeddedGameVersion = null;
            if( GetEmbeddedGameData( ref gameURL, ref gameTitle, ref embeddedGameVersion ) && embeddedGameVersion != null )
            {
                ExtractAndInstallEmbeddedGame( gameTitle, embeddedGameVersion );
            }

            // Download
            if( gameURL != null )
            {
                // Downlaod the RSS file
                Console.Write( "Checking for updates... " );
                var rssFile = DownloadRSS( gameURL );

                // Get title, version and download URL from it
                bool isLatest = false;
                var entry = GetMatchingEntry( rssFile, ref gameTitle, ref gameVersion, ref isLatest );
                string downloadURL = entry != null ? entry.Link : null;
                if( downloadURL != null )
                {
                    Console.WriteLine( "OK." );
                    if( !DownloadAndInstall( downloadURL, gameTitle, gameVersion, isLatest ) )
                    {
                        Console.WriteLine( "Update failed." );
                    }
                }
                else
                {
                    Console.WriteLine( "None found." );
                }
            }

            // Launch
            if( gameTitle != null )
            {
                // Build the path
                string gamePath = GetStorageDirectory();
                gamePath = Path.Combine( gamePath, "Games" );
                gamePath = Path.Combine( gamePath, gameTitle );

                // Determine the version to run
                if( gameVersion == null && File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
                {
                    gameVersion = File.ReadAllText( Path.Combine( gamePath, "LatestVersion.txt" ) ).Trim();
                }

                if( gameVersion != null )
                {
                    // Run the version
                    string versionPath = gamePath;
                    versionPath = Path.Combine( versionPath, "Versions" );
                    versionPath = Path.Combine( versionPath, gameVersion );
                    if( Directory.Exists( versionPath ) )
                    {
                        Console.Write( "Launching game... " );
                        if( Launcher.LaunchGame( versionPath, gameTitle ) )
                        {
                            Console.WriteLine( "OK" );
                        }
                        else
                        {
                            Console.WriteLine( "Failed." );
                        }
                    }
                    else
                    {
                        Console.WriteLine( "Specified version not found." );
                    }
                }
                else
                {
                    Console.WriteLine( "No version specified." );
                }
            }
            else
            {
                Console.WriteLine( "No game specified." );
            }
		}
	}
}

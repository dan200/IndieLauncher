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
        private static extern int uname( IntPtr buf );

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
                        
        private static void SetupEmbeddedAssemblies()
        {
            EmbeddedAssembly.Load( "Ionic.Zip.dll" );
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args )
            {
                return EmbeddedAssembly.Get( args.Name );
            };
        }

        private static string GetDownloadURL( RSSFile file, ref string io_game, ref string io_version, out bool o_isLatest )
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
                                        return entry.Link;
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
            }
            o_isLatest = default( bool );
            return null;
        }

        private static bool Download( string gameTitle, string gameVersion, string downloadURL )
        {
            // Download
            if( !Installer.IsGameDownloaded( gameTitle, gameVersion ) )
            {
                Console.Write( "Downloading update... " );
                if( Installer.DownloadGame( gameTitle, gameVersion, downloadURL ) )
                {
                    Console.WriteLine( "OK." );
                    return true;
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
                return true;
            }
        }

        private static bool Install( string gameTitle, string gameVersion )
        {
            // Install
            if( !Installer.IsGameInstalled( gameTitle, gameVersion ) )
            {
                Console.Write( "Installing update... " );
                if( Installer.InstallGame( gameTitle, gameVersion ) )
                {
                    Console.WriteLine( "OK." );
                    return true;
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
                return true;
            }
        }

        private static void Record( string gameTitle, string gameVersion, bool overwrite )
        {
            // Record that this file is the latest
            var gamePath = Installer.GetBasePath( gameTitle );
            if( overwrite || !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
            }
        }

        private static bool ExtractEmbedded( string gameTitle, string gameVersion )
        {
            // Extract the embedded game
            if( !Installer.IsGameDownloaded( gameTitle, gameVersion ) )
            {                   
                Console.Write( "Extracting embedded game... " );
                if( Installer.ExtractEmbeddedGame() )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }
            return true;
        }

        private static bool InstallEmbedded( string gameTitle, string gameVersion )
        {
            // Install the embedded game
            if( !Installer.IsGameInstalled( gameTitle, gameVersion ) )
            {
                Console.Write( "Installing embedded game... " );
                if( Installer.InstallGame( gameTitle, gameVersion ) )
                {
                    Console.WriteLine( "OK." );
                }
                else
                {
                    Console.WriteLine( "Failed." );
                    return false;
                }
            }
            return true;
        }

		public static void Main( string[] args )
		{
            // Init
            SetupEmbeddedAssemblies();
            Platform = DeterminePlatform();
            Arguments = new ProgramArguments( args );

            // Install the embedded game
            string gameTitle, gameVersion, gameURL;
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( Installer.GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                if( embeddedGameVersion != null &&
                    ExtractEmbedded( embeddedGameTitle, embeddedGameVersion ) &&
                    InstallEmbedded( embeddedGameTitle, embeddedGameVersion ) )
                {
                    Record( embeddedGameTitle, embeddedGameVersion, false );
                }

                gameTitle = embeddedGameTitle;
                gameVersion = Arguments.GetString( "version" );
                gameURL = embeddedGameURL;
            }
            else
            {
                gameTitle = Arguments.GetString( "game" );
                gameVersion = Arguments.GetString( "version" );
                gameURL = null;
            }

            // Check the game URL for updates
            if( gameURL != null )
            {
                // Downlaod the RSS file
                Console.Write( "Checking for updates... " );
                var rssFile = RSSFile.Download( gameURL );

                // Extract information from it
                bool isLatest;
                var downloadURL = GetDownloadURL( rssFile, ref gameTitle, ref gameVersion, out isLatest );
                if( downloadURL != null )
                {
                    Console.WriteLine( "OK." );

                    // Download and install the new version
                    if( Download( gameTitle, gameVersion, downloadURL ) && Install( gameTitle, gameVersion ) )
                    {
                        Record( gameTitle, gameVersion, isLatest );
                    }
                }
                else
                {
                    Console.WriteLine( "None found." );
                }
            }

            // Launch what we've managed to download
            if( gameTitle != null )
            {
                // Determine the version to run
                string gamePath = Installer.GetBasePath( gameTitle );
                if( gameVersion == null && File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
                {
                    gameVersion = File.ReadAllText( Path.Combine( gamePath, "LatestVersion.txt" ) ).Trim();
                }

                if( gameVersion != null )
                {
                    // Run the version
                    if( Installer.IsGameInstalled( gameTitle, gameVersion ) )
                    {
                        Console.Write( "Launching game... " );
                        if( Launcher.LaunchGame( gameTitle, gameVersion ) )
                        {
                            Console.WriteLine( "OK." );
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

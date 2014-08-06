using System;
using Dan200.Launcher.RSS;
using System.Net;
using System.IO;
using System.Diagnostics;
using Ionic.Zip;
using System.Runtime.InteropServices;
using Dan200.Launcher.Util;
using System.Reflection;
using System.Globalization;
using Dan200.Launcher.Interface.Console;
using Dan200.Launcher.Interface.GTK;

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

        public static Language Language
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
            EmbeddedAssembly.Load( "MonoMac.dll" );
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args )
            {
                return EmbeddedAssembly.Get( args.Name );
            };
        }

        private static bool ExtractGameInfo( RSSFile rssFile, string gameTitle, ref string io_version, out bool o_versionIsLatest, out string o_description, out string o_downloadURL )
        {
            if( rssFile != null )
            {
                // Find a matching title
                foreach( var channel in rssFile.Channels )
                {
                    if( channel.Title == gameTitle )
                    {
                        // Find a matching entry
                        for( int i=0; i<channel.Entries.Count; ++i )
                        {
                            var entry = channel.Entries[ i ];
                            if( entry.Title != null && entry.Link != null )
                            {
                                if( io_version == null || entry.Title == io_version )
                                {
                                    io_version = entry.Title;
                                    o_versionIsLatest = (i == 0);
                                    o_description = (channel.Description != null) ? channel.Description : entry.Title;
                                    o_downloadURL = entry.Link;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            o_versionIsLatest = false;
            o_description = null;
            o_downloadURL = null;
            return false;
        }

        private static bool Download( string gameTitle, string gameDescription, string gameVersion, string downloadURL )
        {
            // Download
            if( !Installer.IsGameDownloaded( gameTitle, gameVersion ) )
            {
                Console.Write( "Downloading update... " );
                if( Installer.DownloadGame( gameTitle, gameVersion, downloadURL, delegate( int progress ) {
                    Console.Write( "{0}%", progress );
                } ) )
                {
                    Console.WriteLine( "OK" );
                    return true;
                }
                else
                {
                    Console.WriteLine( "Failed" );
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
                    Console.WriteLine( "OK" );
                    return true;
                }
                else
                {
                    Console.WriteLine( "Failed" );
                    return false;
                }
            }
            else
            {
                Console.WriteLine( "Update already installed." );
                return true;
            }
        }

        private static void RecordLatestVersion( string gameTitle, string gameVersion, bool overwrite )
        {
            // Record that this file is the latest
            var gamePath = Installer.GetBasePath( gameTitle );
            if( overwrite || !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
            }
        }

        private static bool InstallEmbeddedGame()
        {
            // Extract the embedded game
            string gameTitle, gameVersion, updateURL;
            if( Installer.GetEmbeddedGame( out gameTitle, out gameVersion, out updateURL ) )
            {
                if( gameVersion != null )
                {
                    // Extract
                    if( !Installer.IsGameDownloaded( gameTitle, gameVersion ) )
                    {                   
                        Console.Write( "Extracting embedded game... " );
                        if( !Installer.ExtractEmbeddedGame( delegate( int progress ) {
                            Console.Write( "{0}%", progress );
                        } ) )
                        {
                            Console.WriteLine( "Failed" );
                            return false;
                        }
                        else
                        {
                            Console.WriteLine( "OK" );
                        }
                    }

                    // Install
                    if( !Installer.IsGameInstalled( gameTitle, gameVersion ) )
                    {
                        Console.Write( "Installing embedded game... " );
                        if( !Installer.InstallGame( gameTitle, gameVersion ) )
                        {
                            Console.WriteLine( "Failed" );
                            return false;
                        }
                        Console.WriteLine( "OK" );
                        RecordLatestVersion( gameTitle, gameVersion, false );
                    }

                    return true;
                }
            }
            return false;
        }

        public static Language DetermineLanguage()
        {
            Dan200.Launcher.Util.Language.LoadAll();
            return Dan200.Launcher.Util.Language.GetMostSimilarTo(
                CultureInfo.CurrentUICulture.Name.Replace( '-', '_' )
            );
        }

		public static void Main( string[] args )
		{
            // Init
            SetupEmbeddedAssemblies();
            Platform = DeterminePlatform();
            Arguments = new ProgramArguments( args );
            Language = DetermineLanguage();

            // Run UI
            GTKInterface.Run();
            return;

            // Install the embedded game
            InstallEmbeddedGame();

            // Determine which game and which version to run
            string gameTitle, gameVersion, updateURL;
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( Installer.GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                // Run game from embedded game info
                gameTitle = embeddedGameTitle;
                gameVersion = Arguments.GetString( "version" );
                updateURL = embeddedGameURL;
            }
            else
            {
                // Run game from command line
                gameTitle = Arguments.GetString( "game" );
                gameVersion = Arguments.GetString( "version" );
                updateURL = null;
            }
                
            // Abort if no game specified
            if( gameTitle == null )
            {
                Console.WriteLine( "No game specified." );
                return;
            }

            // If a version is specified and already installed, run it
            if( gameVersion != null && Installer.IsGameInstalled( gameTitle, gameVersion ) )
            {
                Console.Write( "Launching game... " );
                if( GameLauncher.LaunchGame( gameTitle, gameVersion ) )
                {
                    Console.WriteLine( "OK" );
                }
                else
                {
                    Console.WriteLine( "Failed" );
                }
                return;
            }

            // Check for updates
            if( updateURL != null )
            {
                // Download the RSS file
                Console.Write( "Checking for updates... " );
                var rssFile = RSSFile.Download( updateURL, delegate(int percentage) {
                    Console.Write( "{0}%", percentage );
                } );

                // Extract information from it
                bool gameVersionIsStrict = (gameVersion != null);
                bool gameVersionIsLatest;
                string gameDescription;
                string downloadURL;
                if( ExtractGameInfo( rssFile, gameTitle, ref gameVersion, out gameVersionIsLatest, out gameDescription, out downloadURL ) )
                {
                    Console.WriteLine( "OK" );

                    // Determine whether to download an update
                    bool downloadUpdate = false;
                    var latestVersion = Installer.GetLatestInstalledVersion( gameTitle );
                    if( gameVersionIsStrict || latestVersion == null )
                    {
                        downloadUpdate = !Installer.IsGameInstalled( gameTitle, gameVersion );
                    }
                    else
                    {
                        downloadUpdate = !Installer.IsGameInstalled( gameTitle, gameVersion ) && false; /*Dialogs.PromptForUpdate( gameDescription );*/
                    }

                    // Download the update
                    if( downloadUpdate &&
                        Download( gameTitle, gameDescription, gameVersion, downloadURL ) &&
                        Install( gameTitle, gameVersion ) )
                    {
                        RecordLatestVersion( gameTitle, gameVersion, gameVersionIsLatest );
                    }
                    if( !gameVersionIsStrict )
                    {
                        gameVersion = null;
                    }
                }
                else
                {
                    Console.WriteLine( "Failed" );
                }
            }

            {
                // Determine the version to run
                if( gameVersion == null )
                {
                    gameVersion = Installer.GetLatestInstalledVersion( gameTitle );
                    if( gameVersion == null || !Installer.IsGameInstalled( gameTitle, gameVersion ) )
                    {
                        Console.WriteLine( "Unable to determine version to run." );
                        return;
                    }
                }

                // Run that version
                if( Installer.IsGameInstalled( gameTitle, gameVersion ) )
                {
                    Console.Write( "Launching game... " );
                    if( GameLauncher.LaunchGame( gameTitle, gameVersion ) )
                    {
                        Console.WriteLine( "OK" );
                    }
                    else
                    {
                        Console.WriteLine( "Failed" );
                    }
                }
                else
                {
                    Console.WriteLine( "Requested version not installed." );
                }
            }
		}
	}
}

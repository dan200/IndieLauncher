using System;
using Dan200.Launcher.RSS;
using System.Net;
using System.IO;
using System.Diagnostics;
using Ionic.Zip;
using System.Runtime.InteropServices;
using Dan200.Launcher.Util;
using System.Reflection;
using Dan200.Launcher.GUI;

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
            EmbeddedAssembly.Load( "MonoMac.dll" );
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args )
            {
                return EmbeddedAssembly.Get( args.Name );
            };
        }

        private static string GetDownloadURL( RSSFile file, string targetGameTitle, string targetGameVersion, out string o_gameTitle, out string o_gameDescription, out string o_gameVersion, out bool o_isLatest )
        {
            if( file != null )
            {
                foreach( var channel in file.Channels )
                {
                    if( channel.Title != null )
                    {
                        if( targetGameTitle == null || channel.Title == targetGameTitle )
                        {
                            o_gameTitle = channel.Title;
                            o_gameDescription = (channel.Description != null) ? channel.Description : channel.Title;
                            for( int i=0; i<channel.Entries.Count; ++i )
                            {
                                var entry = channel.Entries[ i ];
                                if( entry.Title != null )
                                {
                                    if( targetGameVersion == null || entry.Title == targetGameVersion )
                                    {
                                        o_gameVersion = entry.Title;
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
            o_gameTitle = default( string );
            o_gameDescription = default( string );
            o_gameVersion = default( string );
            o_isLatest = default( bool );
            return null;
        }

        private static bool Download( string gameTitle, string gameDescription, string gameVersion, string downloadURL )
        {
            // Download
            if( !Installer.IsGameDownloaded( gameTitle, gameVersion ) )
            {
                Console.Write( "Downloading update... " );
                var progressWindow = Dialogs.CreateDownloadWindow( gameDescription );
                if( Installer.DownloadGame( gameTitle, gameVersion, downloadURL, delegate( int progress ) {
                    progressWindow.SetProgress( progress );
                } ) )
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

        private static void RecordLatestVersion( string gameTitle, string gameVersion, bool overwrite )
        {
            // Record that this file is the latest
            var gamePath = Installer.GetBasePath( gameTitle );
            if( overwrite || !File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                File.WriteAllText( Path.Combine( gamePath, "LatestVersion.txt" ), gameVersion );
            }
        }

        public static string GetLatestVersion( string gameTitle )
        {
            var gamePath = Installer.GetBasePath( gameTitle );
            if( File.Exists( Path.Combine( gamePath, "LatestVersion.txt" ) ) )
            {
                return File.ReadAllText( Path.Combine( gamePath, "LatestVersion.txt" ) ).Trim();
            }
            return null;
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
            Dialogs.Init();

            // Install the embedded game
            string gameTitle, targetGameVersion, gameURL;
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( Installer.GetEmbeddedGame( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                if( embeddedGameVersion != null &&
                    ExtractEmbedded( embeddedGameTitle, embeddedGameVersion ) &&
                    InstallEmbedded( embeddedGameTitle, embeddedGameVersion ) )
                {
                    RecordLatestVersion( embeddedGameTitle, embeddedGameVersion, false );
                }

                gameTitle = embeddedGameTitle;
                targetGameVersion = Arguments.GetString( "version" );
                gameURL = embeddedGameURL;
            }
            else
            {
                gameTitle = Arguments.GetString( "game" );
                targetGameVersion = Arguments.GetString( "version" );
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
                string downloadGameVersion;
                string gameDescription;
                var downloadURL = GetDownloadURL( rssFile, gameTitle, targetGameVersion, out gameTitle, out gameDescription, out downloadGameVersion, out isLatest );
                if( downloadURL != null )
                {
                    Console.WriteLine( "OK." );

                    // Download and install the new version
                    bool update = true;
                    if( !Installer.IsGameInstalled( gameTitle, downloadGameVersion ) )
                    {
                        var latestVersion = GetLatestVersion( gameTitle );
                        if( latestVersion != null )
                        {
                            update = Dialogs.PromptForUpdate( gameDescription );
                        }
                    }
                    if( update )
                    {
                        if( Download( gameTitle, gameDescription, downloadGameVersion, downloadURL ) && Install( gameTitle, downloadGameVersion ) )
                        {
                            targetGameVersion = downloadGameVersion;
                            RecordLatestVersion( gameTitle, downloadGameVersion, isLatest );
                        }
                    }
                    else
                    {
                        targetGameVersion = null;
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
                string gameVersion;
                if( targetGameVersion != null )
                {
                    gameVersion = targetGameVersion;
                }
                else
                {
                    gameVersion = GetLatestVersion( gameTitle );
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

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
            AppDomain.CurrentDomain.AssemblyResolve += delegate(object sender, ResolveEventArgs args )
            {
                return EmbeddedAssembly.Get( args.Name );
            };
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
        }
	}
}

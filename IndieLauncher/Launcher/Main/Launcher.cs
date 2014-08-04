using System;
using System.IO;
using System.Diagnostics;

namespace Dan200.Launcher.Main
{
    public class Launcher
    {
        public static bool LaunchGame( string path, string gameTitle )
        {
            if( Directory.Exists( path ) )
            {
                // Search for a platform suitable exe to run
                string launchPath = null;
                switch( Program.Platform )
                {
                    case Platform.Windows:
                    {
                        string exePath = Path.Combine( path, gameTitle + ".exe" );
                        if( File.Exists( exePath ) )
                        {
                            launchPath = exePath;
                            break;
                        }
                        string batPath = Path.Combine( path, gameTitle + ".bat" );
                        if( File.Exists( batPath ) )
                        {
                            launchPath = batPath;
                            break;
                        }
                        break;
                    }
                    case Platform.OSX:
                    {
                        string appPath = Path.Combine( path, gameTitle + ".app" );
                        if( Directory.Exists( appPath ) )
                        {
                            launchPath = appPath;
                            break;
                        }
                        string shPath = Path.Combine( path, gameTitle + ".sh" );
                        if( File.Exists( shPath ) )
                        {
                            launchPath = shPath;
                            break;
                        }
                        break;
                    }
                    case Platform.Linux:
                    default:
                    {
                        string shPath = Path.Combine( path, gameTitle + ".sh" );
                        if( File.Exists( shPath ) )
                        {
                            launchPath = shPath;
                            break;
                        }
                        break;
                    }
                }

                if( launchPath != null )
                {
                    // Run the platform exe
                    if( Path.GetExtension( launchPath ) == ".sh" )
                    {
                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = "/bin/sh";
                        startInfo.Arguments = Path.GetFileName( launchPath );
                        startInfo.WorkingDirectory = path;
                        Process.Start( startInfo );
                    }
                    else
                    {
                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = launchPath;
                        startInfo.WorkingDirectory = path;
                        Process.Start( startInfo );
                    }
                }
                else
                {
                    // Just open the folder
                    Process.Start( path );
                }
                return true;
            }
            return false;
        }
    }
}


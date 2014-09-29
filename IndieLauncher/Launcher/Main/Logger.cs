using System;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Dan200.Launcher.Main
{
    public static class Logger
    {
        #if DEBUG
        public static bool DebugBuild = true;
        #else
        public static bool DebugBuild = false;
        #endif

        private static StringBuilder s_log = new StringBuilder();

        public static void Log( string text )
        {
            lock( s_log )
            {
                s_log.AppendLine( text );
                if( DebugBuild )
                {
                    Debug.WriteLine( text );
                }
                else
                {
                    Console.WriteLine( text );
                }
            }
        }

        public static void Log( string text, params object[] args )
        {
            Log( string.Format( text, args ) );
        }

        public static void DebugLog( string text )
        {
            if( DebugBuild )
            {
                Log( text );
            }
        }

        public static void DebugLog( string text, params object[] args )
        {
            if( DebugBuild )
            {
                Log( string.Format( text, args ) );
            }
        }

        public static void Save()
        {
            string logPath;
            try
            {
                // Build the path
                logPath = Installer.GetBasePath();
                logPath = Path.Combine( logPath, "Logs" );
                logPath = Path.Combine( logPath, DateTime.Now.ToString( "s" ).Replace( ":", "-" ) + ".txt" );

                // Prepare the directory
                var logDirectory = Path.GetDirectoryName( logPath );
                if( !Directory.Exists( logDirectory ) )
                {
                    // Create the log file directory
                    Directory.CreateDirectory( logDirectory );
                }
                else
                {
                    // Delete old log files from the directory
                    var directoryInfo = new DirectoryInfo( logDirectory );
                    var oldFiles = directoryInfo.EnumerateFiles()
                        .Where( file => file.Extension == ".txt" )
                        .OrderByDescending( file => file.CreationTime )
                        .Skip( 4 );
                    foreach( var file in oldFiles.ToList() )
                    {
                        file.Delete();
                    }
                }
            }
            catch( Exception )
            {
                logPath = "Log.txt";
            }

            // Write the log
            try
            {
                Log( "Writing log file to {0}", logPath );
                lock( s_log )
                {
                    File.WriteAllText( logPath, s_log.ToString() );
                }
            }
            catch( Exception )
            {
                Log( "Failed to write log file to {0}", logPath );
            }
        }
    }
}


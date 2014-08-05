using System;

namespace Dan200.Launcher.GUI
{
    public static class ConsoleDialogs
    {
        public static void Init()
        {
        }

        public static bool PromptForUpdate( string gameTitle )
        {
            Console.Write( "A new version of " + gameTitle + " is available, would you like to update? " );
            string result;
            while( true )
            {
                result = Console.ReadLine();
                if( result == null )
                {
                    return false;
                }
                else
                {
                    result = result.ToLowerInvariant();
                }
                if( result == "y" || result == "yes" )
                {
                    return true;
                }
                else if( result == "n" || result == "no" )
                {
                    return false;
                }
                else
                {
                    Console.Write( "Enter \"yes\" or \"no\": " );
                }
            }
        }

        private class ConsoleProgressWindow : IProgressWindow
        {
            public ConsoleProgressWindow()
            {
            }

            public void SetProgress( int percentage )
            {
                Console.Write( "{0}% ", percentage );
            }
        }

        public static IProgressWindow CreateDownloadWindow( string gameTitle )
        {
            return new ConsoleProgressWindow();
        }
    }
}


using System;
using System.Windows.Forms;
using Dan200.Launcher.Main;
using System.Windows.Forms.VisualStyles;

namespace Dan200.Launcher.GUI
{
    public static class Dialogs
    {
        public static void Init()
        {
            switch( Program.Platform )
            {
                case Platform.Windows:
                {
                    WinFormsDialogs.Init();
                    break;
                }
                case Platform.OSX:
                {
                    MonoMacDialogs.Init();
                    break;
                }
                default:
                {
                    ConsoleDialogs.Init();
                    break;
                }
            }
        }

        public static bool PromptForUpdate( string gameTitle )
        {
            switch( Program.Platform )
            {
                case Platform.Windows:
                {
                    return WinFormsDialogs.PromptForUpdate( gameTitle );
                }
                case Platform.OSX:
                {
                    return MonoMacDialogs.PromptForUpdate( gameTitle );
                }
                default:
                {
                    return ConsoleDialogs.PromptForUpdate( gameTitle );
                }
            }
        }

        public static IProgressWindow CreateDownloadWindow( string gameTitle )
        {
            switch( Program.Platform )
            {
                case Platform.Windows:
                {
                    return WinFormsDialogs.CreateDownloadWindow( gameTitle );
                }
                case Platform.OSX:
                {
                    return WinFormsDialogs.CreateDownloadWindow( gameTitle );
                }
                default:
                {
                    return ConsoleDialogs.CreateDownloadWindow( gameTitle );
                }
            }
        }
    }
}


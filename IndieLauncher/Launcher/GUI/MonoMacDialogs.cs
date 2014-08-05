using System;
using MonoMac.AppKit;
using System.Drawing;
using System.Threading;

namespace Dan200.Launcher.GUI
{
    public static class MonoMacDialogs
    {
        public static void Init()
        {
            NSApplication.Init();
        }

        public static bool PromptForUpdate( string gameTitle )
        {
            var alert = new NSAlert();
            alert.MessageText = gameTitle + " Launcher";
            alert.InformativeText = "A new version of " + gameTitle + " is available, would you like to update?";
            alert.AlertStyle = NSAlertStyle.Informational;

            int yesID = alert.AddButton( "Yes" ).IntValue;
            int noID = alert.AddButton( "No" ).IntValue;

            int result = alert.RunModal(); 
            return (result == (int)NSAlertButtonReturn.First);
        }

        public static IProgressWindow CreateDownloadWindow( string gameTitle )
        {
            return ConsoleDialogs.CreateDownloadWindow( gameTitle );
        }
    }
}


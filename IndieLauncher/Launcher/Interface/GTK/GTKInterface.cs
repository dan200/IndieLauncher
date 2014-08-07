using System;
using Gtk;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.GTK
{
    public class GTKInterface
    {
        public static void Run()
        {
            // Init framework
            Application.Init();

            // Determine which game and which version to run
            string gameTitle, gameVersion, updateURL;
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( Installer.GetEmbeddedGameInfo( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                // Run game from embedded game info
                gameTitle = null;//embeddedGameTitle;
                gameVersion = Program.Arguments.GetString( "version" );
                updateURL = embeddedGameURL;
            }
            else
            {
                // Run game from command line
                gameTitle = Program.Arguments.GetString( "game" );
                gameVersion = Program.Arguments.GetString( "version" );
                updateURL = null;
            }

            // Show the appropriate window
            if( gameTitle != null )
            {
                ShowUpdateWindow( gameTitle, gameVersion, updateURL );
            }
            else if( Installer.GetInstalledGames().Length > 0 )
            {
                ShowGameListWindow();
            }
            else
            {
                ShowErrorWindow( "No games installed. Exiting." );
            }
        }

        public static void ShowUpdateWindow( string gameTitle, string optionalGameVersion, string optionalUpdateURL )
        {
            var updateWindow = new UpdateWindow( gameTitle, optionalGameVersion, optionalUpdateURL );
            updateWindow.ShowAll();
            Application.Run();
        }

        public static void ShowGameListWindow()
        {
            var gameListWindow = new GameListWindow();
            gameListWindow.ShowAll();
            Application.Run();
        }

        public static void ShowErrorWindow( string errorMessage, Window parentWindow=null )
        {
            var dialog = new MessageDialog(
                parentWindow,
                DialogFlags.Modal,
                MessageType.Error,
                ButtonsType.Ok,
                errorMessage
            );
            dialog.WindowPosition = WindowPosition.Center;
            dialog.Show();
            dialog.Run();
            dialog.Hide();
        }
    }
}


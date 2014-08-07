using System;
using System.Windows.Forms;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.WinForms
{
    public class WinFormsInterface
    {
        public static void Run()
        {
            // Init
            Application.EnableVisualStyles();

            // Determine which game and which version to run
            string gameTitle, gameVersion, updateURL;
            string embeddedGameTitle, embeddedGameVersion, embeddedGameURL;
            if( Installer.GetEmbeddedGameInfo( out embeddedGameTitle, out embeddedGameVersion, out embeddedGameURL ) )
            {
                // Run game from embedded game info
                gameTitle = embeddedGameTitle;
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
            else
            {
                ShowErrorWindow( "No game specified. Exiting." );
            }
        }


        public static void ShowUpdateWindow( string gameTitle, string optionalGameVersion, string optionalUpdateURL )
        {
            var form = new UpdateForm( gameTitle, optionalGameVersion, optionalUpdateURL );
            form.ShowDialog();
        }

        public static void ShowErrorWindow( string errorMessage, Form parentForm=null )
        {
            MessageBox.Show(
                parentForm,
                errorMessage,
                "IndieLauncher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}


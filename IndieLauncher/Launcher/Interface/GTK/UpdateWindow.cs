using System;
using Gtk;
using System.Threading.Tasks;
using System.Threading;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.GTK
{
    public class UpdateWindow : Window
    {
        private GameUpdater m_updater;
        private ProgressBar m_progressBar;

        public UpdateWindow( string gameTitle, string optionalGameVersion, string optionalUpdateURL ) : base( WindowType.Toplevel )
        {
            m_updater = new GameUpdater( gameTitle, optionalGameVersion, optionalUpdateURL );
            m_updater.StageChanged += OnStageChanged;
            m_updater.ProgressChanged += OnProgressChanged;
            m_updater.PromptChanged += OnPromptChanged;
            Build();
        }

        private void OnShown( object sender, EventArgs e )
        {
            m_updater.Start();
        }

        private void OnStageChanged( object sender, EventArgs e )
        {
            var stage = m_updater.Stage;
            Application.Invoke( delegate
            {
                string status = stage.GetStatus( Program.Language );
                System.Console.WriteLine( status );
                this.Title = status;
                if( stage == GameUpdateStage.Finished ||
                    stage == GameUpdateStage.Cancelled )
                {
                    Application.Quit();
                }
            } );
        }

        private void OnProgressChanged( object sender, EventArgs e )
        {
            var progress = m_updater.StageProgress;
            Application.Invoke( delegate
            {
                int percentage = (int)(progress * 100.0);
                m_progressBar.Text = string.Format( "{0}%", percentage );
                m_progressBar.Fraction = progress;
            } );
        }

        private void OnPromptChanged( object sender, EventArgs e )
        {
            var prompt = m_updater.CurrentPrompt;
            var description = m_updater.GameDescription;
            Application.Invoke( delegate
            {
                if( prompt == GameUpdatePrompt.UsernamePassword ||
                    prompt == GameUpdatePrompt.Password )
                {
                    // These prompts are not yet implemented
                    m_updater.AnswerPrompt( false );
                    return;
                }

                // Show message dialog
                var dialog = new MessageDialog(
                    this,
                    DialogFlags.Modal,
                    MessageType.Question,
                    ButtonsType.YesNo,
                    prompt.GetQuestion( Program.Language, description )
                );
                dialog.Show();
                int response = dialog.Run();
                dialog.Destroy();

                // Inform the updater
                if( response == (int)ResponseType.Close ||
                    response == (int)ResponseType.DeleteEvent )
                {
                    m_updater.Cancel();
                    m_updater.AnswerPrompt( false );
                }
                else
                {
                    m_updater.AnswerPrompt( response == (int)ResponseType.Yes );
                }
            } );
        }

        private void OnDeleteEvent( object sender, DeleteEventArgs a )
        {
            var stage = m_updater.Stage;
            if( stage == GameUpdateStage.NotStarted ||
                stage == GameUpdateStage.Finished ||
                stage == GameUpdateStage.Cancelled ||
                stage == GameUpdateStage.Failed )
            {
                Application.Quit();
            }
            else
            {
                m_updater.Cancel();
                a.RetVal = true;
            }
        }

        private void Build()
        {
            this.Title = "";
            this.BorderWidth = 6;
            this.WindowPosition = WindowPosition.Center;
            this.TypeHint = Gdk.WindowTypeHint.Dialog;

            var vbox = new VBox( false, 4 );

            //var label = new Label ();
            //label.Text = "Checking for updates...";
            //vbox.PackStart (label, false, false, 0 );

            m_progressBar = new ProgressBar();
            m_progressBar.Fraction = 0.0f;
            m_progressBar.Text = "0%";
            vbox.PackStart( m_progressBar, false, false, 0 );

            //var button = new Button ();
            //button.WidthRequest = 100;
            //button.Label = "Cancel";
            //vbox.PackStart (button, false, false, 0);

            this.Add( vbox );

            this.SetDefaultSize( 300, 100 );
            this.SetSizeRequest( 300, -1 );
            this.Resizable = false;

            this.Shown += OnShown;
            this.DeleteEvent += OnDeleteEvent;
        }
    }
}

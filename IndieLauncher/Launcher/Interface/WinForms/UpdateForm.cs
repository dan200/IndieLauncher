using System;
using System.Windows.Forms;
using Dan200.Launcher.Main;
using System.ComponentModel;
using System.Drawing;

namespace Dan200.Launcher.Interface.WinForms
{
    public class UpdateForm : Form
    {
        private GameUpdater m_updater;
        private ProgressBar m_progressBar;

        public UpdateForm( string gameTitle, string optionalGameVersion, string optionalUpdateURL )
        {
            m_updater = new GameUpdater( gameTitle, optionalGameVersion, optionalUpdateURL );
            m_updater.StageChanged += OnStageChanged;
            m_updater.StageChanged += OnProgressChanged;
            m_updater.ProgressChanged += OnProgressChanged;
            m_updater.PromptChanged += OnPromptChanged;
            Build();
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            m_updater.Start();
        }

        private void OnStageChanged( object sender, EventArgs e )
        {
            var stage = m_updater.Stage;
            this.BeginInvoke( (Action)delegate
            {
                string status = stage.GetStatus( Program.Language );
                Console.WriteLine( status );
                if( stage == GameUpdateStage.Finished ||
                    stage == GameUpdateStage.Cancelled )
                {
                    this.Close();
                }
            } );
        }

        private void OnProgressChanged( object sender, EventArgs e )
        {
            var stage = m_updater.Stage;
            var progress = m_updater.StageProgress;
            this.BeginInvoke( (Action)delegate
            {
                int percentage = (int)(progress * 100.0);
                if( stage == GameUpdateStage.NotStarted ||
                    stage == GameUpdateStage.Finished ||
                    stage == GameUpdateStage.Cancelled ||
                    stage == GameUpdateStage.Failed )
                {
                    this.Text = stage.GetStatus( Program.Language );
                }
                else
                {
                    this.Text = stage.GetStatus( Program.Language ) + " (" + percentage + "%)";
                }
                m_progressBar.Value = percentage;
            } );
        }

        private void OnPromptChanged( object sender, EventArgs e )
        {
            var prompt = m_updater.CurrentPrompt;
            var customMessage = m_updater.CustomMessage;
            var description = m_updater.GameDescription;
            var previousUsername = m_updater.PreviouslyEnteredUsername;
            var previousPassword = m_updater.PreviouslyEnteredPassword;
            this.BeginInvoke( (Action)delegate
            {
                if( prompt == GameUpdatePrompt.Username ||
                    prompt == GameUpdatePrompt.Password ||
                    prompt == GameUpdatePrompt.UsernameAndPassword )
                {
                    // Show credentials dialog
                    var dialog = new CredentialsForm(
                        (prompt != GameUpdatePrompt.Password) ?
                            ((previousUsername != null) ? previousUsername : "") :
                            null,
                        (prompt != GameUpdatePrompt.Username) ?
                            ((previousPassword != null) ? previousPassword : "") :
                            null
                    );
                    var result = dialog.ShowDialog( this );

                    // Inform the updater
                    if( result == DialogResult.OK )
                    {
                        m_updater.AnswerPrompt( true, dialog.Username, dialog.Password );
                    }
                    else
                    {
                        m_updater.AnswerPrompt( false );
                    }
                }
                else if( prompt == GameUpdatePrompt.CustomMessage )
                {
                    // Show message dialog
                    MessageBox.Show(
                        this,
                        customMessage,
                        Program.Language.Translate( "window.title", description ),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // Inform the updater
                    m_updater.AnswerPrompt( true );
                }
                else
                {
                    // Show question dialog
                    var result = MessageBox.Show(
                        this,
                        prompt.GetQuestion( Program.Language, description ),
                        Program.Language.Translate( "window.title", description ),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    // Inform the updater
                    if( result == DialogResult.Cancel )
                    {
                        m_updater.Cancel();
                        m_updater.AnswerPrompt( false );
                    }
                    else
                    {
                        m_updater.AnswerPrompt( result == DialogResult.Yes );
                    }
                }
            } );
        }

        protected override void OnClosing( CancelEventArgs e )
        {
            base.OnClosing( e );
            var stage = m_updater.Stage;
            if( stage == GameUpdateStage.NotStarted ||
                stage == GameUpdateStage.Finished ||
                stage == GameUpdateStage.Cancelled ||
                stage == GameUpdateStage.Failed )
            {
                return;
            }
            else
            {
                m_updater.Cancel();
                e.Cancel = true;
            }
        }

        private void Build()
        {
            this.Text = "";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size( 350, 36 );
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.SuspendLayout();

            m_progressBar = new ProgressBar();
            m_progressBar.Minimum = 0;
            m_progressBar.Maximum = 100;
            m_progressBar.Value = 0;
            m_progressBar.Location = new Point( 6, 6 );
            m_progressBar.Size = new Size( this.ClientSize.Width - 12, this.ClientSize.Height - 12 );
            this.Controls.Add( m_progressBar );

            this.ResumeLayout();
        }
    }
}


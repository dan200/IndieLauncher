using System;
using System.Windows.Forms;
using System.Drawing;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.WinForms
{
    public class CredentialsForm : Form
    {
        private TextBox m_usernameBox;
        private TextBox m_passwordBox;

        public string Username
        {
            get
            {
                if( m_usernameBox != null )
                {
                    return m_usernameBox.Text;
                }
                return "";
            }
        }

        public string Password
        {
            get
            {
                if( m_passwordBox != null )
                {
                    return m_passwordBox.Text;
                }
                return "";
            }
        }

        public CredentialsForm( string username, string password )
        {
            Build( username, password );
        }
            
        private void OnOKButtonPressed( object sender, EventArgs args )
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void OnCancelButtonPressed( object sender, EventArgs args )
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void Build( string username, string password )
        {
            this.Text = Program.Language.Translate( "prompt.credentials" );
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ClientSize = new Size( 275, 100 );
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.SuspendLayout();

            int yPos = 6;
            if( username != null )
            {
                m_usernameBox = new TextBox();
                m_usernameBox.Text = username;
                m_usernameBox.Location = new Point( 6 + 75 + 6, yPos );
                m_usernameBox.Width = this.ClientSize.Width - (12 + 75 + 6);
                this.Controls.Add( m_usernameBox );

                var label = new Label();
                label.Text = Program.Language.Translate( "label.username" );
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Width = 75;
                label.Height = m_usernameBox.Height;
                label.Location = new Point( 6, yPos );
                this.Controls.Add( label );

                yPos += m_usernameBox.Height + 6;
            }
            if( password != null )
            {
                m_passwordBox = new TextBox();
                m_passwordBox.Text = password;
                m_passwordBox.Location = new Point( 6 + 75 + 6, yPos );
                m_passwordBox.Width = this.ClientSize.Width - (12 + 75 + 6);
                m_passwordBox.UseSystemPasswordChar = true;
                this.Controls.Add( m_passwordBox );

                var label = new Label();
                label.Text = Program.Language.Translate( "label.password" );
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Width = 75;
                label.Height = m_passwordBox.Height;
                label.Location = new Point( 6, yPos );
                this.Controls.Add( label );

                yPos += m_passwordBox.Height + 6;
            }

            var okButton = new Button();
            okButton.Location = new Point( this.ClientSize.Width - 6 - 80 - 6 - 80, yPos );
            okButton.Width = 80;
            okButton.TextAlign = ContentAlignment.MiddleCenter;
            okButton.Text = Program.Language.Translate( "button.ok" );
            okButton.Click += OnOKButtonPressed;
            this.Controls.Add( okButton );

            var cancelButton = new Button();
            cancelButton.Location = new Point( this.ClientSize.Width - 6 - 80, yPos );
            cancelButton.Width = 80;
            cancelButton.TextAlign = ContentAlignment.MiddleCenter;
            cancelButton.Text = Program.Language.Translate( "button.cancel" );
            cancelButton.Click += OnCancelButtonPressed;
            this.Controls.Add( cancelButton );

            this.AcceptButton = okButton;
            this.ClientSize = new Size( 275, yPos + okButton.Height + 6 );
            this.ResumeLayout();
        }
    }
}


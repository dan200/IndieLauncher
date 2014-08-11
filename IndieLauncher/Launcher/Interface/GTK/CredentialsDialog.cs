using System;
using Gtk;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.GTK
{
    public class CredentialsDialog : Dialog
    {
        private Entry m_usernameEntry;
        private Entry m_passwordEntry;

        public string Username
        {
            get
            {
                if( m_usernameEntry != null )
                {
                    return m_usernameEntry.Text;
                }
                return "";
            }
        }

        public string Password
        {
            get
            {
                if( m_passwordEntry != null )
                {
                    return m_passwordEntry.Text;
                }
                return "";
            }
        }

        public CredentialsDialog( Window parent, string username, string password ) : base(
            Program.Language.Translate( "prompt.credentials" ),
            parent, DialogFlags.Modal,
            Program.Language.Translate( "button.cancel" ), ResponseType.Cancel,
            Program.Language.Translate( "button.ok" ), ResponseType.Ok )
        {
            this.DefaultResponse = ResponseType.Ok;
            Build( username, password );
        }

        private void Build( string username, string password )
        {
            //this.Title = "";
            //this.BorderWidth = 6;
            this.WindowPosition = WindowPosition.Center;
            //this.TypeHint = Gdk.WindowTypeHint.Dialog;

            var vbox = new VBox( true, 4 );
            vbox.BorderWidth = 4;

            if( username != null )
            {
                var hbox = new HBox( false, 6 );

                var label = new Label();
                label.Text = Program.Language.Translate( "label.username" );
                label.Xalign = 0.0f;
                label.WidthRequest = 70;
                hbox.PackStart( label, false, false, 0 );

                m_usernameEntry = new Entry();
                m_usernameEntry.Text = username;
                hbox.PackStart( m_usernameEntry, true, true, 0 );

                vbox.PackStart( hbox, false, false, 0 );
            }

            if( password != null )
            {
                var hbox = new HBox( false, 6 );

                var label = new Label();
                label.Text = Program.Language.Translate( "label.password" );
                label.Xalign = 0.0f;
                label.WidthRequest = 70;
                hbox.PackStart( label, false, false, 0 );

                m_passwordEntry = new Entry();
                m_passwordEntry.Text = password;
                m_passwordEntry.Visibility = false;
                hbox.PackStart( m_passwordEntry, true, true, 0 );

                vbox.PackStart( hbox, false, false, 0 );
            }

            this.VBox.PackStart( vbox );

            this.SetDefaultSize( 275, 50 );
            this.SetSizeRequest( 275, -1 );
            this.Resizable = false;
        }
    }
}


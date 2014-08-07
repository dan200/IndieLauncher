using System;
using Gtk;
using Dan200.Launcher.Main;

namespace Dan200.Launcher.Interface.GTK
{
    public class GameListWindow : Window
    {
        public GameListWindow() : base( WindowType.Toplevel )
        {
            Build();
        }

        private void OnShown( object sender, EventArgs e )
        {
        }

        private void OnDeleteEvent( object sender, DeleteEventArgs a )
        {
            Application.Quit();
        }

        private void Build()
        {
            this.Title = "Select Game";
            this.BorderWidth = 6;
            this.WindowPosition = WindowPosition.Center;
            this.TypeHint = Gdk.WindowTypeHint.Dialog;

            var vbox = new VBox( false, 4 );

            //var label = new Label ();
            //label.Text = "Checking for updates...";
            //vbox.PackStart (label, false, false, 0 );

            var combo = new ComboBox( Installer.GetInstalledGames() );
            combo.Active = 0;
            vbox.PackStart( combo, false, false, 0 );

            var button = new Button ();
            button.Label = "Launch";
            button.Clicked += delegate( object sender, EventArgs args )
            {
                this.HideAll();

                var updateWindow = new UpdateWindow( combo.ActiveText, null, null );
                updateWindow.ShowAll();
            };
            vbox.PackStart( button, false, false, 0 );

            this.Add( vbox );

            this.SetDefaultSize( 300, 100 );
            this.SetSizeRequest( 300, -1 );
            this.Resizable = false;

            this.Shown += OnShown;
            this.DeleteEvent += OnDeleteEvent;
        }
    }
}


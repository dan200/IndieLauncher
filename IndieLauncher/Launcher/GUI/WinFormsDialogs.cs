using System;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Drawing;

namespace Dan200.Launcher.GUI
{
    public class WinFormsDialogs
    {
        public static void Init()
        {
            Application.EnableVisualStyles();
            Application.VisualStyleState = VisualStyleState.ClientAndNonClientAreasEnabled;
        }

        public static bool PromptForUpdate( string gameTitle )
        {
            var result = MessageBox.Show(
                "A new version of " + gameTitle + " is available, would you like to update?",
                gameTitle + " Launcher",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );
            return (result == DialogResult.Yes);
        }

        public static IProgressWindow CreateDownloadWindow( string gameTitle )
        {
            return ConsoleDialogs.CreateDownloadWindow( gameTitle );

            /*
            // WIP
            var form = new Form();
            form.Text = "Updating " + gameTitle;
            form.ClientSize = new Size( 300, 8 + 16 + 8 + 24 + 8 + 24 + 8 );
            form.FormBorderStyle = FormBorderStyle.FixedSingle;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.SuspendLayout();

            var label = new Label();
            label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label.Location = new Point( 8, 8 );
            label.Width = form.ClientSize.Width - 16;
            label.Height = 16;
            label.Text = "Redirection.zip";
            label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            form.Controls.Add( label );

            var progress = new ProgressBar();
            progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progress.Location = new Point( 8, 8 + 16 + 8 );
            progress.Width = form.ClientSize.Width - 16;
            progress.Height = 24;
            progress.Minimum = 0;
            progress.Maximum = 100;
            progress.Value = 30;
            form.Controls.Add( progress );

            var cancel = new Button();
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cancel.Location = new Point( form.ClientSize.Width - 8 - 96, 8 + 16 + 8 + 24 + 8 );
            cancel.Width = 96;
            cancel.Height = 24;
            cancel.Text = "Cancel";
            form.Controls.Add( cancel );

            form.ResumeLayout();
            form.ShowDialog();
            return null;
            */
        }
    }
}


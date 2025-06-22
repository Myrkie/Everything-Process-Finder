using System.Media;
using Everything_Process_Finder.Utils;

namespace Everything_Process_Finder.Forms
{
    public sealed class MessageBox : Form
    {
        private MessageBox(string message, string title = "Message")
        {
            Text = title;
            Size = new Size(400, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            Icon = AppResources.AppIcon;

            Label messageLabel = new Label();
            messageLabel.Text = message;
            messageLabel.AutoSize = false;
            messageLabel.Size = new Size(360, 50);
            messageLabel.Location = new Point(20, 20);
            messageLabel.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(messageLabel);

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Size = new Size(75, 25);
            okButton.Location = new Point((ClientSize.Width - okButton.Width) / 2, 80);
            okButton.Anchor = AnchorStyles.Bottom;
            okButton.Click += (_, _) => { Close(); };
            Controls.Add(okButton);
        }

        public static void Show(string message, string title = "Message")
        {
            var msgBox = new MessageBox(message, title);
            SystemSounds.Exclamation.Play();
            msgBox.ShowDialog();
            msgBox.Focus();
        }
    }
}
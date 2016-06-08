using iTppc.Classes;
using iTppc.Core;
using iTppc.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iTppc.Forms
{
    public partial class Login : Form
    {
        private Game _game;
        private Settings _settings;
        private string _settingsPath;

        public Login()
        {
            InitializeComponent();

            _game = new Game();
            _settingsPath = string.Format(@"{0}\{1}", Application.StartupPath, "Settings.ini");
            _settings = new Settings(_settingsPath);

            if (_settings.Username != null && _settings.Password != null)
            {
                usernameTextBox.Text = _settings.Username;
                passwordTextBox.Text = _settings.Password;
                rememberCredentialsCheckBox.Checked = true;
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                loginButton.PerformClick();
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(usernameTextBox.Text) || string.IsNullOrEmpty(passwordTextBox.Text))
            {
                MessageBox.Show("The username and password fields cannot be left empty.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Thread t = new Thread(() =>
            {
                disableControls(true);

                if (_game.login(usernameTextBox.Text, passwordTextBox.Text))
                {
                    rememberCredentialsCheckBox.safeInvoke(() =>
                    {
                        if (rememberCredentialsCheckBox.Checked)
                        {
                            _settings.Username = usernameTextBox.Text;
                            _settings.Password = passwordTextBox.Text;
                            _settings.save();
                        }
                        else
                        {
                            if (File.Exists(_settingsPath)) 
                                File.Delete(_settingsPath);
                        }
                    });

                    this.safeInvoke(() => { Close(); });
                    Application.Run(new Main(_game, _settings));
                }
                else
                {
                    disableControls(false);
                    MessageBox.Show("The username or password is incorrect.", string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }

        private void disableControls(bool disable)
        {
            usernameTextBox.safeInvoke(() => { usernameTextBox.ReadOnly = disable; });
            passwordTextBox.safeInvoke(() => { passwordTextBox.ReadOnly = disable; });
            rememberCredentialsCheckBox.safeInvoke(() => { rememberCredentialsCheckBox.Enabled = !disable; });
            loginButton.safeInvoke(() => { loginButton.Enabled = !disable; });
        }
    }
}

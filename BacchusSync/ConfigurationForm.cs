using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace pGina.Plugin.BacchusSync
{
    public partial class ConfigurationForm : Form
    {
        public ConfigurationForm()
        {
            InitializeComponent();

            profileServerAddressText.Text = Settings.ProfileServerAddress;
            profileServerPortText.Text = Settings.ProfileServerPort.ToString();
            hostKeyText.Text = Settings.HostKey;
            profileServerBaseDirectoryText.Text = Settings.ProfileServerBaseDirectory;
            authServerAddressText.Text = Settings.AuthenticationServerAddress;
        }

        private void OnClickOkButton(object sender, EventArgs e)
        {
            if (!ushort.TryParse(profileServerPortText.Text, out ushort profileServerPort))
            {
                MessageBox.Show("Invalid server port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (hostKeyText.Text != string.Empty)
            {
                try
                {
                    Convert.FromBase64String(hostKeyText.Text);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Host key is not a valid base64 string", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Settings.ProfileServerAddress = profileServerAddressText.Text;
            Settings.ProfileServerPort = profileServerPort;
            Settings.HostKey = hostKeyText.Text;
            Settings.ProfileServerBaseDirectory = profileServerBaseDirectoryText.Text;
            Settings.AuthenticationServerAddress = authServerAddressText.Text;
            Close();
        }

        private void OnClickCancelButton(object sender, EventArgs e)
        {
            Close();
        }

        private void OnClickGetHostKey(object sender, EventArgs eventArgs)
        {
            try
            {
                string serverAddress = profileServerAddressText.Text;

                if (serverAddress == string.Empty)
                {
                    MessageBox.Show("Server address is empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!ushort.TryParse(profileServerPortText.Text, out ushort port))
                {
                    MessageBox.Show("Invalid port.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (var client = new SshClient(serverAddress, port, "test-user", "test-password"))
                {
                    client.HostKeyReceived += OnHostKeyReceived;
                    client.Connect();
                }
            }
            catch (SshConnectionException)
            {
                // Ignore exception caused by setting CanTrust to false.
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, e.GetType().ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnHostKeyReceived(object sender, HostKeyEventArgs e)
        {
            string encodedHostKey = Convert.ToBase64String(e.HostKey);
            hostKeyText.Text = encodedHostKey;
            e.CanTrust = false;
        }
    }
}

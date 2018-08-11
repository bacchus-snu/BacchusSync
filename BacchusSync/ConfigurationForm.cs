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

            serverAddressText.Text = Settings.ServerAddress;
            serverPortText.Text = Settings.ServerPort.ToString();
            serverFingerprintText.Text = Settings.ServerFingerprint;
            serverBaseDirectoryText.Text = Settings.ServerBaseDirectory;

        }

        private void OnClickOkButton(object sender, EventArgs e)
        {
            if (!ushort.TryParse(serverPortText.Text, out ushort port))
            {
                MessageBox.Show("Invalid server port", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Settings.ServerAddress = serverAddressText.Text;
            Settings.ServerPort = port;
            Settings.ServerFingerprint = serverFingerprintText.Text;
            Settings.ServerBaseDirectory = serverBaseDirectoryText.Text;
            Close();
        }

        private void OnClickCancelButton(object sender, EventArgs e)
        {
            Close();
        }
    }
}

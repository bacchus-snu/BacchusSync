namespace pGina.Plugin.BacchusSync
{
    partial class ConfigurationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.serverAddressText = new System.Windows.Forms.TextBox();
            this.serverPortText = new System.Windows.Forms.TextBox();
            this.hostKeyText = new System.Windows.Forms.TextBox();
            this.serverBaseDirectoryText = new System.Windows.Forms.TextBox();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.getHostKeyButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(91, 12);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server address";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 12);
            this.label2.TabIndex = 1;
            this.label2.Text = "Server port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 24);
            this.label3.TabIndex = 2;
            this.label3.Text = "Host key\r\n(Empty to ignore)";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 125);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(126, 12);
            this.label4.TabIndex = 3;
            this.label4.Text = "Server base directory";
            // 
            // serverAddressText
            // 
            this.serverAddressText.Location = new System.Drawing.Point(144, 12);
            this.serverAddressText.Name = "serverAddressText";
            this.serverAddressText.Size = new System.Drawing.Size(257, 21);
            this.serverAddressText.TabIndex = 4;
            // 
            // serverPortText
            // 
            this.serverPortText.Location = new System.Drawing.Point(144, 39);
            this.serverPortText.Name = "serverPortText";
            this.serverPortText.Size = new System.Drawing.Size(257, 21);
            this.serverPortText.TabIndex = 5;
            // 
            // hostKeyText
            // 
            this.hostKeyText.Location = new System.Drawing.Point(144, 66);
            this.hostKeyText.Name = "hostKeyText";
            this.hostKeyText.Size = new System.Drawing.Size(257, 21);
            this.hostKeyText.TabIndex = 6;
            // 
            // serverBaseDirectoryText
            // 
            this.serverBaseDirectoryText.Location = new System.Drawing.Point(144, 122);
            this.serverBaseDirectoryText.Name = "serverBaseDirectoryText";
            this.serverBaseDirectoryText.Size = new System.Drawing.Size(257, 21);
            this.serverBaseDirectoryText.TabIndex = 7;
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(245, 149);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 8;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.OnClickOkButton);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(326, 149);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 9;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.OnClickCancelButton);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Enabled = false;
            this.label5.Location = new System.Drawing.Point(85, 98);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(223, 12);
            this.label5.TabIndex = 10;
            this.label5.Text = "Base64 encoded server\'s rsa host key";
            // 
            // getHostKeyButton
            // 
            this.getHostKeyButton.Location = new System.Drawing.Point(314, 93);
            this.getHostKeyButton.Name = "getHostKeyButton";
            this.getHostKeyButton.Size = new System.Drawing.Size(87, 23);
            this.getHostKeyButton.TabIndex = 11;
            this.getHostKeyButton.Text = "Get host key";
            this.getHostKeyButton.UseVisualStyleBackColor = true;
            this.getHostKeyButton.Click += new System.EventHandler(this.OnClickGetHostKey);
            // 
            // ConfigurationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(413, 183);
            this.Controls.Add(this.getHostKeyButton);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.serverBaseDirectoryText);
            this.Controls.Add(this.hostKeyText);
            this.Controls.Add(this.serverPortText);
            this.Controls.Add(this.serverAddressText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "ConfigurationForm";
            this.Text = "ConfigurationForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox serverAddressText;
        private System.Windows.Forms.TextBox serverPortText;
        private System.Windows.Forms.TextBox hostKeyText;
        private System.Windows.Forms.TextBox serverBaseDirectoryText;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button getHostKeyButton;
    }
}
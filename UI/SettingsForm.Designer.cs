namespace LoginGuardUI;

partial class SettingsForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.TabControl tabControl;
    private System.Windows.Forms.TabPage tabTelegram;
    private System.Windows.Forms.TabPage tabCamera;
    private System.Windows.Forms.TabPage tabStorage;
    private System.Windows.Forms.TabPage tabService;

    // Telegram Tab Controls
    private System.Windows.Forms.Label lblBotToken;
    private System.Windows.Forms.TextBox txtBotToken;
    private System.Windows.Forms.CheckBox chkShowToken;
    private System.Windows.Forms.Label lblChatId;
    private System.Windows.Forms.TextBox txtChatId;
    private System.Windows.Forms.Button btnTestTelegram;
    private System.Windows.Forms.Label lblTelegramStatus;

    // Camera Tab Controls
    private System.Windows.Forms.Label lblCamera;
    private System.Windows.Forms.ComboBox cboCamera;
    private System.Windows.Forms.Button btnTestCapture;
    private System.Windows.Forms.PictureBox picPreview;
    private System.Windows.Forms.Label lblCameraStatus;

    // Storage Tab Controls
    private System.Windows.Forms.Label lblCaptureRetention;
    private System.Windows.Forms.ComboBox cboCaptureRetention;
    private System.Windows.Forms.Label lblLogRetention;
    private System.Windows.Forms.ComboBox cboLogRetention;
    private System.Windows.Forms.Label lblStorageNote;

    // Service Tab Controls
    private System.Windows.Forms.Label lblServiceStatusTitle;
    private System.Windows.Forms.Label lblServiceStatus;
    private System.Windows.Forms.Button btnStartService;
    private System.Windows.Forms.Button btnStopService;
    private System.Windows.Forms.Button btnRestartService;
    private System.Windows.Forms.Button btnRefreshStatus;

    // Bottom Action Buttons
    private System.Windows.Forms.Button btnSave;
    private System.Windows.Forms.Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.tabControl = new System.Windows.Forms.TabControl();
        this.tabTelegram = new System.Windows.Forms.TabPage();
        this.lblTelegramStatus = new System.Windows.Forms.Label();
        this.btnTestTelegram = new System.Windows.Forms.Button();
        this.chkShowToken = new System.Windows.Forms.CheckBox();
        this.txtChatId = new System.Windows.Forms.TextBox();
        this.lblChatId = new System.Windows.Forms.Label();
        this.txtBotToken = new System.Windows.Forms.TextBox();
        this.lblBotToken = new System.Windows.Forms.Label();
        this.tabCamera = new System.Windows.Forms.TabPage();
        this.lblCameraStatus = new System.Windows.Forms.Label();
        this.picPreview = new System.Windows.Forms.PictureBox();
        this.btnTestCapture = new System.Windows.Forms.Button();
        this.cboCamera = new System.Windows.Forms.ComboBox();
        this.lblCamera = new System.Windows.Forms.Label();
        this.tabStorage = new System.Windows.Forms.TabPage();
        this.lblStorageNote = new System.Windows.Forms.Label();
        this.cboLogRetention = new System.Windows.Forms.ComboBox();
        this.lblLogRetention = new System.Windows.Forms.Label();
        this.cboCaptureRetention = new System.Windows.Forms.ComboBox();
        this.lblCaptureRetention = new System.Windows.Forms.Label();
        this.tabService = new System.Windows.Forms.TabPage();
        this.btnRefreshStatus = new System.Windows.Forms.Button();
        this.btnRestartService = new System.Windows.Forms.Button();
        this.btnStopService = new System.Windows.Forms.Button();
        this.btnStartService = new System.Windows.Forms.Button();
        this.lblServiceStatus = new System.Windows.Forms.Label();
        this.lblServiceStatusTitle = new System.Windows.Forms.Label();
        this.btnSave = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.tabControl.SuspendLayout();
        this.tabTelegram.SuspendLayout();
        this.tabCamera.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
        this.tabStorage.SuspendLayout();
        this.tabService.SuspendLayout();
        this.SuspendLayout();
        // 
        // tabControl
        // 
        this.tabControl.Controls.Add(this.tabTelegram);
        this.tabControl.Controls.Add(this.tabCamera);
        this.tabControl.Controls.Add(this.tabStorage);
        this.tabControl.Controls.Add(this.tabService);
        this.tabControl.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.tabControl.Location = new System.Drawing.Point(12, 12);
        this.tabControl.Name = "tabControl";
        this.tabControl.SelectedIndex = 0;
        this.tabControl.Size = new System.Drawing.Size(560, 390);
        this.tabControl.TabIndex = 0;
        // 
        // tabTelegram
        // 
        this.tabTelegram.BackColor = System.Drawing.Color.White;
        this.tabTelegram.Controls.Add(this.lblTelegramStatus);
        this.tabTelegram.Controls.Add(this.btnTestTelegram);
        this.tabTelegram.Controls.Add(this.chkShowToken);
        this.tabTelegram.Controls.Add(this.txtChatId);
        this.tabTelegram.Controls.Add(this.lblChatId);
        this.tabTelegram.Controls.Add(this.txtBotToken);
        this.tabTelegram.Controls.Add(this.lblBotToken);
        this.tabTelegram.Location = new System.Drawing.Point(4, 26);
        this.tabTelegram.Name = "tabTelegram";
        this.tabTelegram.Padding = new System.Windows.Forms.Padding(15);
        this.tabTelegram.Size = new System.Drawing.Size(552, 360);
        this.tabTelegram.TabIndex = 0;
        this.tabTelegram.Text = "Telegram Alerting";
        // 
        // lblTelegramStatus
        // 
        this.lblTelegramStatus.AutoSize = true;
        this.lblTelegramStatus.Location = new System.Drawing.Point(18, 250);
        this.lblTelegramStatus.Name = "lblTelegramStatus";
        this.lblTelegramStatus.Size = new System.Drawing.Size(0, 17);
        this.lblTelegramStatus.TabIndex = 6;
        // 
        // btnTestTelegram
        // 
        this.btnTestTelegram.BackColor = System.Drawing.Color.FromArgb(240, 244, 248);
        this.btnTestTelegram.FlatStyle = System.Windows.Forms.FlatStyle.System;
        this.btnTestTelegram.Location = new System.Drawing.Point(18, 205);
        this.btnTestTelegram.Name = "btnTestTelegram";
        this.btnTestTelegram.Size = new System.Drawing.Size(190, 32);
        this.btnTestTelegram.TabIndex = 5;
        this.btnTestTelegram.Text = "🔔 Test Telegram Connection";
        this.btnTestTelegram.UseVisualStyleBackColor = false;
        this.btnTestTelegram.Click += new System.EventHandler(this.btnTestTelegram_Click);
        // 
        // chkShowToken
        // 
        this.chkShowToken.AutoSize = true;
        this.chkShowToken.Location = new System.Drawing.Point(18, 85);
        this.chkShowToken.Name = "chkShowToken";
        this.chkShowToken.Size = new System.Drawing.Size(130, 21);
        this.chkShowToken.TabIndex = 2;
        this.chkShowToken.Text = "Show Bot Token";
        this.chkShowToken.UseVisualStyleBackColor = true;
        this.chkShowToken.CheckedChanged += new System.EventHandler(this.chkShowToken_CheckedChanged);
        // 
        // txtChatId
        // 
        this.txtChatId.Location = new System.Drawing.Point(18, 145);
        this.txtChatId.Name = "txtChatId";
        this.txtChatId.Size = new System.Drawing.Size(510, 24);
        this.txtChatId.TabIndex = 4;
        // 
        // lblChatId
        // 
        this.lblChatId.AutoSize = true;
        this.lblChatId.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblChatId.Location = new System.Drawing.Point(15, 125);
        this.lblChatId.Name = "lblChatId";
        this.lblChatId.Size = new System.Drawing.Size(126, 17);
        this.lblChatId.TabIndex = 3;
        this.lblChatId.Text = "Authorized Chat ID:";
        // 
        // txtBotToken
        // 
        this.txtBotToken.Location = new System.Drawing.Point(18, 45);
        this.txtBotToken.Name = "txtBotToken";
        this.txtBotToken.Size = new System.Drawing.Size(510, 24);
        this.txtBotToken.TabIndex = 1;
        this.txtBotToken.UseSystemPasswordChar = true;
        // 
        // lblBotToken
        // 
        this.lblBotToken.AutoSize = true;
        this.lblBotToken.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblBotToken.Location = new System.Drawing.Point(15, 25);
        this.lblBotToken.Name = "lblBotToken";
        this.lblBotToken.Size = new System.Drawing.Size(127, 17);
        this.lblBotToken.TabIndex = 0;
        this.lblBotToken.Text = "Telegram Bot Token:";
        // 
        // tabCamera
        // 
        this.tabCamera.BackColor = System.Drawing.Color.White;
        this.tabCamera.Controls.Add(this.lblCameraStatus);
        this.tabCamera.Controls.Add(this.picPreview);
        this.tabCamera.Controls.Add(this.btnTestCapture);
        this.tabCamera.Controls.Add(this.cboCamera);
        this.tabCamera.Controls.Add(this.lblCamera);
        this.tabCamera.Location = new System.Drawing.Point(4, 26);
        this.tabCamera.Name = "tabCamera";
        this.tabCamera.Padding = new System.Windows.Forms.Padding(15);
        this.tabCamera.Size = new System.Drawing.Size(552, 360);
        this.tabCamera.TabIndex = 1;
        this.tabCamera.Text = "Camera Capture";
        // 
        // lblCameraStatus
        // 
        this.lblCameraStatus.AutoSize = true;
        this.lblCameraStatus.Location = new System.Drawing.Point(15, 325);
        this.lblCameraStatus.Name = "lblCameraStatus";
        this.lblCameraStatus.Size = new System.Drawing.Size(0, 17);
        this.lblCameraStatus.TabIndex = 4;
        // 
        // picPreview
        // 
        this.picPreview.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
        this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.picPreview.Location = new System.Drawing.Point(18, 95);
        this.picPreview.Name = "picPreview";
        this.picPreview.Size = new System.Drawing.Size(320, 220);
        this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
        this.picPreview.TabIndex = 3;
        this.picPreview.TabStop = false;
        // 
        // btnTestCapture
        // 
        this.btnTestCapture.Location = new System.Drawing.Point(360, 45);
        this.btnTestCapture.Name = "btnTestCapture";
        this.btnTestCapture.Size = new System.Drawing.Size(165, 28);
        this.btnTestCapture.TabIndex = 2;
        this.btnTestCapture.Text = "📷 Test Capture";
        this.btnTestCapture.UseVisualStyleBackColor = true;
        this.btnTestCapture.Click += new System.EventHandler(this.btnTestCapture_Click);
        // 
        // cboCamera
        // 
        this.cboCamera.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboCamera.FormattingEnabled = true;
        this.cboCamera.Location = new System.Drawing.Point(18, 46);
        this.cboCamera.Name = "cboCamera";
        this.cboCamera.Size = new System.Drawing.Size(320, 24);
        this.cboCamera.TabIndex = 1;
        // 
        // lblCamera
        // 
        this.lblCamera.AutoSize = true;
        this.lblCamera.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblCamera.Location = new System.Drawing.Point(15, 25);
        this.lblCamera.Name = "lblCamera";
        this.lblCamera.Size = new System.Drawing.Size(102, 17);
        this.lblCamera.TabIndex = 0;
        this.lblCamera.Text = "Selected Camera:";
        // 
        // tabStorage
        // 
        this.tabStorage.BackColor = System.Drawing.Color.White;
        this.tabStorage.Controls.Add(this.lblStorageNote);
        this.tabStorage.Controls.Add(this.cboLogRetention);
        this.tabStorage.Controls.Add(this.lblLogRetention);
        this.tabStorage.Controls.Add(this.cboCaptureRetention);
        this.tabStorage.Controls.Add(this.lblCaptureRetention);
        this.tabStorage.Location = new System.Drawing.Point(4, 26);
        this.tabStorage.Name = "tabStorage";
        this.tabStorage.Padding = new System.Windows.Forms.Padding(15);
        this.tabStorage.Size = new System.Drawing.Size(552, 360);
        this.tabStorage.TabIndex = 2;
        this.tabStorage.Text = "Storage & Retention";
        // 
        // lblStorageNote
        // 
        this.lblStorageNote.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic);
        this.lblStorageNote.ForeColor = System.Drawing.Color.DimGray;
        this.lblStorageNote.Location = new System.Drawing.Point(18, 200);
        this.lblStorageNote.Name = "lblStorageNote";
        this.lblStorageNote.Size = new System.Drawing.Size(510, 80);
        this.lblStorageNote.TabIndex = 4;
        this.lblStorageNote.Text = "• A background task cleans old photos daily.\r\n• Selecting 'Never' keeps evidence photos permanently.\r\n• Log files exceeding the log retention window are automatically archived with a .old timestamp.";
        // 
        // cboLogRetention
        // 
        this.cboLogRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboLogRetention.FormattingEnabled = true;
        this.cboLogRetention.Location = new System.Drawing.Point(18, 140);
        this.cboLogRetention.Name = "cboLogRetention";
        this.cboLogRetention.Size = new System.Drawing.Size(320, 24);
        this.cboLogRetention.TabIndex = 3;
        // 
        // lblLogRetention
        // 
        this.lblLogRetention.AutoSize = true;
        this.lblLogRetention.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblLogRetention.Location = new System.Drawing.Point(15, 115);
        this.lblLogRetention.Name = "lblLogRetention";
        this.lblLogRetention.Size = new System.Drawing.Size(155, 17);
        this.lblLogRetention.TabIndex = 2;
        this.lblLogRetention.Text = "Service Log Retention:";
        // 
        // cboCaptureRetention
        // 
        this.cboCaptureRetention.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboCaptureRetention.FormattingEnabled = true;
        this.cboCaptureRetention.Location = new System.Drawing.Point(18, 50);
        this.cboCaptureRetention.Name = "cboCaptureRetention";
        this.cboCaptureRetention.Size = new System.Drawing.Size(320, 24);
        this.cboCaptureRetention.TabIndex = 1;
        // 
        // lblCaptureRetention
        // 
        this.lblCaptureRetention.AutoSize = true;
        this.lblCaptureRetention.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblCaptureRetention.Location = new System.Drawing.Point(15, 25);
        this.lblCaptureRetention.Name = "lblCaptureRetention";
        this.lblCaptureRetention.Size = new System.Drawing.Size(176, 17);
        this.lblCaptureRetention.TabIndex = 0;
        this.lblCaptureRetention.Text = "Webcam Capture Retention:";
        // 
        // tabService
        // 
        this.tabService.BackColor = System.Drawing.Color.White;
        this.tabService.Controls.Add(this.btnRefreshStatus);
        this.tabService.Controls.Add(this.btnRestartService);
        this.tabService.Controls.Add(this.btnStopService);
        this.tabService.Controls.Add(this.btnStartService);
        this.tabService.Controls.Add(this.lblServiceStatus);
        this.tabService.Controls.Add(this.lblServiceStatusTitle);
        this.tabService.Location = new System.Drawing.Point(4, 26);
        this.tabService.Name = "tabService";
        this.tabService.Padding = new System.Windows.Forms.Padding(15);
        this.tabService.Size = new System.Drawing.Size(552, 360);
        this.tabService.TabIndex = 3;
        this.tabService.Text = "Service Control";
        // 
        // btnRefreshStatus
        // 
        this.btnRefreshStatus.Location = new System.Drawing.Point(18, 185);
        this.btnRefreshStatus.Name = "btnRefreshStatus";
        this.btnRefreshStatus.Size = new System.Drawing.Size(140, 32);
        this.btnRefreshStatus.TabIndex = 5;
        this.btnRefreshStatus.Text = "🔄 Refresh Status";
        this.btnRefreshStatus.UseVisualStyleBackColor = true;
        this.btnRefreshStatus.Click += new System.EventHandler(this.btnRefreshStatus_Click);
        // 
        // btnRestartService
        // 
        this.btnRestartService.Location = new System.Drawing.Point(310, 110);
        this.btnRestartService.Name = "btnRestartService";
        this.btnRestartService.Size = new System.Drawing.Size(140, 35);
        this.btnRestartService.TabIndex = 4;
        this.btnRestartService.Text = "🔄 Restart Service";
        this.btnRestartService.UseVisualStyleBackColor = true;
        this.btnRestartService.Click += new System.EventHandler(this.btnRestartService_Click);
        // 
        // btnStopService
        // 
        this.btnStopService.Location = new System.Drawing.Point(164, 110);
        this.btnStopService.Name = "btnStopService";
        this.btnStopService.Size = new System.Drawing.Size(140, 35);
        this.btnStopService.TabIndex = 3;
        this.btnStopService.Text = "⏹ Stop Service";
        this.btnStopService.UseVisualStyleBackColor = true;
        this.btnStopService.Click += new System.EventHandler(this.btnStopService_Click);
        // 
        // btnStartService
        // 
        this.btnStartService.Location = new System.Drawing.Point(18, 110);
        this.btnStartService.Name = "btnStartService";
        this.btnStartService.Size = new System.Drawing.Size(140, 35);
        this.btnStartService.TabIndex = 2;
        this.btnStartService.Text = "▶ Start Service";
        this.btnStartService.UseVisualStyleBackColor = true;
        this.btnStartService.Click += new System.EventHandler(this.btnStartService_Click);
        // 
        // lblServiceStatus
        // 
        this.lblServiceStatus.AutoSize = true;
        this.lblServiceStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
        this.lblServiceStatus.ForeColor = System.Drawing.Color.Gray;
        this.lblServiceStatus.Location = new System.Drawing.Point(18, 55);
        this.lblServiceStatus.Name = "lblServiceStatus";
        this.lblServiceStatus.Size = new System.Drawing.Size(81, 21);
        this.lblServiceStatus.TabIndex = 1;
        this.lblServiceStatus.Text = "Checking...";
        // 
        // lblServiceStatusTitle
        // 
        this.lblServiceStatusTitle.AutoSize = true;
        this.lblServiceStatusTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblServiceStatusTitle.Location = new System.Drawing.Point(15, 25);
        this.lblServiceStatusTitle.Name = "lblServiceStatusTitle";
        this.lblServiceStatusTitle.Size = new System.Drawing.Size(206, 17);
        this.lblServiceStatusTitle.TabIndex = 0;
        this.lblServiceStatusTitle.Text = "LoginGuardService Runtime Status:";
        // 
        // btnSave
        // 
        this.btnSave.BackColor = System.Drawing.Color.FromArgb(16, 124, 65);
        this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.System;
        this.btnSave.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
        this.btnSave.Location = new System.Drawing.Point(346, 415);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new System.Drawing.Size(110, 32);
        this.btnSave.TabIndex = 1;
        this.btnSave.Text = "💾 Save";
        this.btnSave.UseVisualStyleBackColor = true;
        this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(462, 415);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(110, 32);
        this.btnCancel.TabIndex = 2;
        this.btnCancel.Text = "Cancel";
        this.btnCancel.UseVisualStyleBackColor = true;
        // 
        // SettingsForm
        // 
        this.AcceptButton = this.btnSave;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.FromArgb(248, 249, 250);
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(584, 461);
        this.Controls.Add(this.btnCancel);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.tabControl);
        this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "SettingsForm";
        this.ShowInTaskbar = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "LoginGuard Settings";
        this.Load += new System.EventHandler(this.SettingsForm_Load);
        this.tabControl.ResumeLayout(false);
        this.tabTelegram.ResumeLayout(false);
        this.tabTelegram.PerformLayout();
        this.tabCamera.ResumeLayout(false);
        this.tabCamera.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
        this.tabStorage.ResumeLayout(false);
        this.tabStorage.PerformLayout();
        this.tabService.ResumeLayout(false);
        this.tabService.PerformLayout();
        this.ResumeLayout(false);
    }
}

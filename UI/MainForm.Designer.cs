namespace LoginGuardUI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Panel pnlHeader;
    private System.Windows.Forms.Label lblAppTitle;
    private System.Windows.Forms.Label lblAppSubtitle;
    private System.Windows.Forms.Label lblHeaderServiceStatus;

    private System.Windows.Forms.Panel pnlToolbar;
    private System.Windows.Forms.Button btnOpenCaptures;
    private System.Windows.Forms.Button btnSettings;
    private System.Windows.Forms.Button btnRefreshLog;
    private System.Windows.Forms.Button btnClearView;

    private System.Windows.Forms.ListView lvwActivity;
    private System.Windows.Forms.ColumnHeader colTime;
    private System.Windows.Forms.ColumnHeader colEvent;
    private System.Windows.Forms.ColumnHeader colRaw;

    private System.Windows.Forms.StatusStrip statusStrip;
    private System.Windows.Forms.ToolStripStatusLabel lblStatusInfo;
    private System.Windows.Forms.ToolStripStatusLabel lblEventCount;

    private System.Windows.Forms.Timer refreshTimer;

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
        this.components = new System.ComponentModel.Container();
        this.pnlHeader = new System.Windows.Forms.Panel();
        this.lblHeaderServiceStatus = new System.Windows.Forms.Label();
        this.lblAppSubtitle = new System.Windows.Forms.Label();
        this.lblAppTitle = new System.Windows.Forms.Label();
        this.pnlToolbar = new System.Windows.Forms.Panel();
        this.btnClearView = new System.Windows.Forms.Button();
        this.btnRefreshLog = new System.Windows.Forms.Button();
        this.btnSettings = new System.Windows.Forms.Button();
        this.btnOpenCaptures = new System.Windows.Forms.Button();
        this.lvwActivity = new System.Windows.Forms.ListView();
        this.colTime = new System.Windows.Forms.ColumnHeader();
        this.colEvent = new System.Windows.Forms.ColumnHeader();
        this.colRaw = new System.Windows.Forms.ColumnHeader();
        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.lblStatusInfo = new System.Windows.Forms.ToolStripStatusLabel();
        this.lblEventCount = new System.Windows.Forms.ToolStripStatusLabel();
        this.refreshTimer = new System.Windows.Forms.Timer(this.components);
        this.pnlHeader.SuspendLayout();
        this.pnlToolbar.SuspendLayout();
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();
        // 
        // pnlHeader
        // 
        this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(24, 32, 44);
        this.pnlHeader.Controls.Add(this.lblHeaderServiceStatus);
        this.pnlHeader.Controls.Add(this.lblAppSubtitle);
        this.pnlHeader.Controls.Add(this.lblAppTitle);
        this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlHeader.Location = new System.Drawing.Point(0, 0);
        this.pnlHeader.Name = "pnlHeader";
        this.pnlHeader.Size = new System.Drawing.Size(884, 65);
        this.pnlHeader.TabIndex = 0;
        // 
        // lblHeaderServiceStatus
        // 
        this.lblHeaderServiceStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.lblHeaderServiceStatus.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
        this.lblHeaderServiceStatus.ForeColor = System.Drawing.Color.LightGreen;
        this.lblHeaderServiceStatus.Location = new System.Drawing.Point(620, 20);
        this.lblHeaderServiceStatus.Name = "lblHeaderServiceStatus";
        this.lblHeaderServiceStatus.Size = new System.Drawing.Size(250, 25);
        this.lblHeaderServiceStatus.TabIndex = 2;
        this.lblHeaderServiceStatus.Text = "● Checking Service...";
        this.lblHeaderServiceStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // 
        // lblAppSubtitle
        // 
        this.lblAppSubtitle.AutoSize = true;
        this.lblAppSubtitle.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblAppSubtitle.ForeColor = System.Drawing.Color.FromArgb(170, 185, 205);
        this.lblAppSubtitle.Location = new System.Drawing.Point(15, 38);
        this.lblAppSubtitle.Name = "lblAppSubtitle";
        this.lblAppSubtitle.Size = new System.Drawing.Size(243, 15);
        this.lblAppSubtitle.TabIndex = 1;
        this.lblAppSubtitle.Text = "Automated Laptop Defense & Telegram Alerts";
        // 
        // lblAppTitle
        // 
        this.lblAppTitle.AutoSize = true;
        this.lblAppTitle.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
        this.lblAppTitle.ForeColor = System.Drawing.Color.White;
        this.lblAppTitle.Location = new System.Drawing.Point(13, 9);
        this.lblAppTitle.Name = "lblAppTitle";
        this.lblAppTitle.Size = new System.Drawing.Size(127, 28);
        this.lblAppTitle.TabIndex = 0;
        this.lblAppTitle.Text = "LoginGuard";
        // 
        // pnlToolbar
        // 
        this.pnlToolbar.BackColor = System.Drawing.Color.FromArgb(243, 245, 248);
        this.pnlToolbar.Controls.Add(this.btnClearView);
        this.pnlToolbar.Controls.Add(this.btnRefreshLog);
        this.pnlToolbar.Controls.Add(this.btnSettings);
        this.pnlToolbar.Controls.Add(this.btnOpenCaptures);
        this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlToolbar.Location = new System.Drawing.Point(0, 65);
        this.pnlToolbar.Name = "pnlToolbar";
        this.pnlToolbar.Padding = new System.Windows.Forms.Padding(10, 8, 10, 8);
        this.pnlToolbar.Size = new System.Drawing.Size(884, 48);
        this.pnlToolbar.TabIndex = 1;
        // 
        // btnClearView
        // 
        this.btnClearView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClearView.Location = new System.Drawing.Point(774, 9);
        this.btnClearView.Name = "btnClearView";
        this.btnClearView.Size = new System.Drawing.Size(95, 30);
        this.btnClearView.TabIndex = 3;
        this.btnClearView.Text = "Clear View";
        this.btnClearView.UseVisualStyleBackColor = true;
        this.btnClearView.Click += new System.EventHandler(this.btnClearView_Click);
        // 
        // btnRefreshLog
        // 
        this.btnRefreshLog.Location = new System.Drawing.Point(340, 9);
        this.btnRefreshLog.Name = "btnRefreshLog";
        this.btnRefreshLog.Size = new System.Drawing.Size(115, 30);
        this.btnRefreshLog.TabIndex = 2;
        this.btnRefreshLog.Text = "🔄 Refresh";
        this.btnRefreshLog.UseVisualStyleBackColor = true;
        this.btnRefreshLog.Click += new System.EventHandler(this.btnRefreshLog_Click);
        // 
        // btnSettings
        // 
        this.btnSettings.Location = new System.Drawing.Point(210, 9);
        this.btnSettings.Name = "btnSettings";
        this.btnSettings.Size = new System.Drawing.Size(120, 30);
        this.btnSettings.TabIndex = 1;
        this.btnSettings.Text = "⚙️ Settings";
        this.btnSettings.UseVisualStyleBackColor = true;
        this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
        // 
        // btnOpenCaptures
        // 
        this.btnOpenCaptures.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
        this.btnOpenCaptures.Location = new System.Drawing.Point(12, 9);
        this.btnOpenCaptures.Name = "btnOpenCaptures";
        this.btnOpenCaptures.Size = new System.Drawing.Size(190, 30);
        this.btnOpenCaptures.TabIndex = 0;
        this.btnOpenCaptures.Text = "📁 Open Captures Folder";
        this.btnOpenCaptures.UseVisualStyleBackColor = true;
        this.btnOpenCaptures.Click += new System.EventHandler(this.btnOpenCaptures_Click);
        // 
        // lvwActivity
        // 
        this.lvwActivity.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colTime,
            this.colEvent,
            this.colRaw});
        this.lvwActivity.Dock = System.Windows.Forms.DockStyle.Fill;
        this.lvwActivity.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.lvwActivity.FullRowSelect = true;
        this.lvwActivity.GridLines = true;
        this.lvwActivity.Location = new System.Drawing.Point(0, 113);
        this.lvwActivity.Name = "lvwActivity";
        this.lvwActivity.Size = new System.Drawing.Size(884, 426);
        this.lvwActivity.TabIndex = 2;
        this.lvwActivity.UseCompatibleStateImageBehavior = false;
        this.lvwActivity.View = System.Windows.Forms.View.Details;
        // 
        // colTime
        // 
        this.colTime.Text = "Timestamp";
        this.colTime.Width = 150;
        // 
        // colEvent
        // 
        this.colEvent.Text = "Activity Event (Plain English)";
        this.colEvent.Width = 380;
        // 
        // colRaw
        // 
        this.colRaw.Text = "Raw Service Details";
        this.colRaw.Width = 330;
        // 
        // statusStrip
        // 
        this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatusInfo,
            this.lblEventCount});
        this.statusStrip.Location = new System.Drawing.Point(0, 539);
        this.statusStrip.Name = "statusStrip";
        this.statusStrip.Size = new System.Drawing.Size(884, 22);
        this.statusStrip.TabIndex = 3;
        // 
        // lblStatusInfo
        // 
        this.lblStatusInfo.Name = "lblStatusInfo";
        this.lblStatusInfo.Size = new System.Drawing.Size(209, 17);
        this.lblStatusInfo.Text = "Log source: C:\\CameraSpikeLog\\service_log.txt";
        // 
        // lblEventCount
        // 
        this.lblEventCount.Name = "lblEventCount";
        this.lblEventCount.Size = new System.Drawing.Size(660, 17);
        this.lblEventCount.Spring = true;
        this.lblEventCount.Text = "0 events";
        this.lblEventCount.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
        // 
        // refreshTimer
        // 
        this.refreshTimer.Interval = 3000;
        this.refreshTimer.Tick += new System.EventHandler(this.refreshTimer_Tick);
        // 
        // MainForm
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.BackColor = System.Drawing.Color.White;
        this.ClientSize = new System.Drawing.Size(884, 561);
        this.Controls.Add(this.lvwActivity);
        this.Controls.Add(this.statusStrip);
        this.Controls.Add(this.pnlToolbar);
        this.Controls.Add(this.pnlHeader);
        this.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.MinimumSize = new System.Drawing.Size(700, 450);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "LoginGuard — Security Monitor";
        this.Load += new System.EventHandler(this.MainForm_Load);
        this.pnlHeader.ResumeLayout(false);
        this.pnlHeader.PerformLayout();
        this.pnlToolbar.ResumeLayout(false);
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}

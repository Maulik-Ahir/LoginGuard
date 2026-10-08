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

    private System.Windows.Forms.TabControl tabMain;
    private System.Windows.Forms.TabPage tabActivityLog;
    private System.Windows.Forms.TabPage tabIncidentHistory;

    private System.Windows.Forms.ListView lvwActivity;
    private System.Windows.Forms.ColumnHeader colTime;
    private System.Windows.Forms.ColumnHeader colEvent;
    private System.Windows.Forms.ColumnHeader colRaw;

    // ── Incident History tab controls ──
    private System.Windows.Forms.Panel pnlHistoryToolbar;
    private System.Windows.Forms.Button btnRefreshHistory;
    private System.Windows.Forms.Button btnOpenCapture;
    private System.Windows.Forms.ComboBox cboHistoryFilter;
    private System.Windows.Forms.Label lblFilterLabel;

    private System.Windows.Forms.SplitContainer splitHistory;
    private System.Windows.Forms.DataGridView dgvIncidents;
    private System.Windows.Forms.Panel pnlDetails;
    private System.Windows.Forms.Label lblDetailsTitle;
    private System.Windows.Forms.TextBox txtDetails;

    private System.Windows.Forms.Panel pnlSummary;
    private System.Windows.Forms.Label lblSummaryStats;
    private System.Windows.Forms.Label lblSummaryLatest;

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

        this.tabMain = new System.Windows.Forms.TabControl();
        this.tabActivityLog = new System.Windows.Forms.TabPage();
        this.tabIncidentHistory = new System.Windows.Forms.TabPage();

        this.lvwActivity = new System.Windows.Forms.ListView();
        this.colTime = new System.Windows.Forms.ColumnHeader();
        this.colEvent = new System.Windows.Forms.ColumnHeader();
        this.colRaw = new System.Windows.Forms.ColumnHeader();

        // Incident History controls
        this.pnlHistoryToolbar = new System.Windows.Forms.Panel();
        this.btnRefreshHistory = new System.Windows.Forms.Button();
        this.btnOpenCapture = new System.Windows.Forms.Button();
        this.cboHistoryFilter = new System.Windows.Forms.ComboBox();
        this.lblFilterLabel = new System.Windows.Forms.Label();
        this.splitHistory = new System.Windows.Forms.SplitContainer();
        this.dgvIncidents = new System.Windows.Forms.DataGridView();
        this.pnlDetails = new System.Windows.Forms.Panel();
        this.lblDetailsTitle = new System.Windows.Forms.Label();
        this.txtDetails = new System.Windows.Forms.TextBox();
        this.pnlSummary = new System.Windows.Forms.Panel();
        this.lblSummaryStats = new System.Windows.Forms.Label();
        this.lblSummaryLatest = new System.Windows.Forms.Label();

        this.statusStrip = new System.Windows.Forms.StatusStrip();
        this.lblStatusInfo = new System.Windows.Forms.ToolStripStatusLabel();
        this.lblEventCount = new System.Windows.Forms.ToolStripStatusLabel();
        this.refreshTimer = new System.Windows.Forms.Timer(this.components);
        this.pnlHeader.SuspendLayout();
        this.pnlToolbar.SuspendLayout();
        this.tabMain.SuspendLayout();
        this.tabActivityLog.SuspendLayout();
        this.tabIncidentHistory.SuspendLayout();
        this.pnlHistoryToolbar.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.splitHistory)).BeginInit();
        this.splitHistory.Panel1.SuspendLayout();
        this.splitHistory.Panel2.SuspendLayout();
        this.splitHistory.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.dgvIncidents)).BeginInit();
        this.pnlDetails.SuspendLayout();
        this.pnlSummary.SuspendLayout();
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
        // tabMain
        // 
        this.tabMain.Controls.Add(this.tabActivityLog);
        this.tabMain.Controls.Add(this.tabIncidentHistory);
        this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
        this.tabMain.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.tabMain.Location = new System.Drawing.Point(0, 113);
        this.tabMain.Name = "tabMain";
        this.tabMain.SelectedIndex = 0;
        this.tabMain.Size = new System.Drawing.Size(884, 426);
        this.tabMain.TabIndex = 2;
        // 
        // tabActivityLog
        // 
        this.tabActivityLog.Controls.Add(this.lvwActivity);
        this.tabActivityLog.Location = new System.Drawing.Point(4, 26);
        this.tabActivityLog.Name = "tabActivityLog";
        this.tabActivityLog.Padding = new System.Windows.Forms.Padding(0);
        this.tabActivityLog.Size = new System.Drawing.Size(876, 396);
        this.tabActivityLog.TabIndex = 0;
        this.tabActivityLog.Text = "📋 Activity Log";
        this.tabActivityLog.UseVisualStyleBackColor = true;
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
        this.lvwActivity.Location = new System.Drawing.Point(0, 0);
        this.lvwActivity.Name = "lvwActivity";
        this.lvwActivity.Size = new System.Drawing.Size(876, 396);
        this.lvwActivity.TabIndex = 0;
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
        // tabIncidentHistory
        // 
        this.tabIncidentHistory.Controls.Add(this.splitHistory);
        this.tabIncidentHistory.Controls.Add(this.pnlSummary);
        this.tabIncidentHistory.Controls.Add(this.pnlHistoryToolbar);
        this.tabIncidentHistory.Location = new System.Drawing.Point(4, 26);
        this.tabIncidentHistory.Name = "tabIncidentHistory";
        this.tabIncidentHistory.Padding = new System.Windows.Forms.Padding(0);
        this.tabIncidentHistory.Size = new System.Drawing.Size(876, 396);
        this.tabIncidentHistory.TabIndex = 1;
        this.tabIncidentHistory.Text = "🔍 Incident History";
        this.tabIncidentHistory.UseVisualStyleBackColor = true;
        // 
        // pnlHistoryToolbar
        // 
        this.pnlHistoryToolbar.BackColor = System.Drawing.Color.FromArgb(243, 245, 248);
        this.pnlHistoryToolbar.Controls.Add(this.btnOpenCapture);
        this.pnlHistoryToolbar.Controls.Add(this.btnRefreshHistory);
        this.pnlHistoryToolbar.Controls.Add(this.cboHistoryFilter);
        this.pnlHistoryToolbar.Controls.Add(this.lblFilterLabel);
        this.pnlHistoryToolbar.Dock = System.Windows.Forms.DockStyle.Top;
        this.pnlHistoryToolbar.Location = new System.Drawing.Point(0, 0);
        this.pnlHistoryToolbar.Name = "pnlHistoryToolbar";
        this.pnlHistoryToolbar.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
        this.pnlHistoryToolbar.Size = new System.Drawing.Size(876, 42);
        this.pnlHistoryToolbar.TabIndex = 0;
        // 
        // lblFilterLabel
        // 
        this.lblFilterLabel.AutoSize = true;
        this.lblFilterLabel.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.lblFilterLabel.Location = new System.Drawing.Point(10, 12);
        this.lblFilterLabel.Name = "lblFilterLabel";
        this.lblFilterLabel.Size = new System.Drawing.Size(36, 15);
        this.lblFilterLabel.TabIndex = 0;
        this.lblFilterLabel.Text = "Filter:";
        // 
        // cboHistoryFilter
        // 
        this.cboHistoryFilter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboHistoryFilter.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cboHistoryFilter.Location = new System.Drawing.Point(50, 8);
        this.cboHistoryFilter.Name = "cboHistoryFilter";
        this.cboHistoryFilter.Size = new System.Drawing.Size(200, 23);
        this.cboHistoryFilter.TabIndex = 1;
        this.cboHistoryFilter.SelectedIndexChanged += new System.EventHandler(this.cboHistoryFilter_SelectedIndexChanged);
        // 
        // btnRefreshHistory
        // 
        this.btnRefreshHistory.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnRefreshHistory.Location = new System.Drawing.Point(265, 7);
        this.btnRefreshHistory.Name = "btnRefreshHistory";
        this.btnRefreshHistory.Size = new System.Drawing.Size(110, 27);
        this.btnRefreshHistory.TabIndex = 2;
        this.btnRefreshHistory.Text = "🔄 Refresh";
        this.btnRefreshHistory.UseVisualStyleBackColor = true;
        this.btnRefreshHistory.Click += new System.EventHandler(this.btnRefreshHistory_Click);
        // 
        // btnOpenCapture
        // 
        this.btnOpenCapture.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.btnOpenCapture.Location = new System.Drawing.Point(385, 7);
        this.btnOpenCapture.Name = "btnOpenCapture";
        this.btnOpenCapture.Size = new System.Drawing.Size(130, 27);
        this.btnOpenCapture.TabIndex = 3;
        this.btnOpenCapture.Text = "📷 Open Capture";
        this.btnOpenCapture.UseVisualStyleBackColor = true;
        this.btnOpenCapture.Click += new System.EventHandler(this.btnOpenCapture_Click);
        // 
        // pnlSummary
        // 
        this.pnlSummary.BackColor = System.Drawing.Color.FromArgb(235, 240, 248);
        this.pnlSummary.Controls.Add(this.lblSummaryStats);
        this.pnlSummary.Controls.Add(this.lblSummaryLatest);
        this.pnlSummary.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.pnlSummary.Location = new System.Drawing.Point(0, 350);
        this.pnlSummary.Name = "pnlSummary";
        this.pnlSummary.Padding = new System.Windows.Forms.Padding(10, 4, 10, 4);
        this.pnlSummary.Size = new System.Drawing.Size(876, 46);
        this.pnlSummary.TabIndex = 2;
        // 
        // lblSummaryStats
        // 
        this.lblSummaryStats.AutoSize = true;
        this.lblSummaryStats.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
        this.lblSummaryStats.Location = new System.Drawing.Point(10, 5);
        this.lblSummaryStats.Name = "lblSummaryStats";
        this.lblSummaryStats.Size = new System.Drawing.Size(200, 15);
        this.lblSummaryStats.TabIndex = 0;
        this.lblSummaryStats.Text = "Total: 0 | Captures: 0 | Sent: 0 | Pending: 0";
        // 
        // lblSummaryLatest
        // 
        this.lblSummaryLatest.AutoSize = true;
        this.lblSummaryLatest.Font = new System.Drawing.Font("Segoe UI", 8.5F);
        this.lblSummaryLatest.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
        this.lblSummaryLatest.Location = new System.Drawing.Point(10, 24);
        this.lblSummaryLatest.Name = "lblSummaryLatest";
        this.lblSummaryLatest.Size = new System.Drawing.Size(150, 15);
        this.lblSummaryLatest.TabIndex = 1;
        this.lblSummaryLatest.Text = "Latest: No incidents recorded";
        // 
        // splitHistory
        // 
        this.splitHistory.Dock = System.Windows.Forms.DockStyle.Fill;
        this.splitHistory.Location = new System.Drawing.Point(0, 42);
        this.splitHistory.Name = "splitHistory";
        this.splitHistory.Orientation = System.Windows.Forms.Orientation.Horizontal;
        // 
        // splitHistory.Panel1 — DataGridView
        // 
        this.splitHistory.Panel1.Controls.Add(this.dgvIncidents);
        // 
        // splitHistory.Panel2 — Details
        // 
        this.splitHistory.Panel2.Controls.Add(this.pnlDetails);
        this.splitHistory.Size = new System.Drawing.Size(876, 308);
        this.splitHistory.SplitterDistance = 190;
        this.splitHistory.TabIndex = 1;
        // 
        // dgvIncidents
        // 
        this.dgvIncidents.AllowUserToAddRows = false;
        this.dgvIncidents.AllowUserToDeleteRows = false;
        this.dgvIncidents.AllowUserToResizeRows = false;
        this.dgvIncidents.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
        this.dgvIncidents.BackgroundColor = System.Drawing.Color.White;
        this.dgvIncidents.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.dgvIncidents.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
        this.dgvIncidents.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        this.dgvIncidents.Dock = System.Windows.Forms.DockStyle.Fill;
        this.dgvIncidents.Font = new System.Drawing.Font("Segoe UI", 9.5F);
        this.dgvIncidents.Location = new System.Drawing.Point(0, 0);
        this.dgvIncidents.MultiSelect = false;
        this.dgvIncidents.Name = "dgvIncidents";
        this.dgvIncidents.ReadOnly = true;
        this.dgvIncidents.RowHeadersVisible = false;
        this.dgvIncidents.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvIncidents.Size = new System.Drawing.Size(876, 190);
        this.dgvIncidents.TabIndex = 0;
        this.dgvIncidents.SelectionChanged += new System.EventHandler(this.dgvIncidents_SelectionChanged);
        // 
        // pnlDetails
        // 
        this.pnlDetails.BackColor = System.Drawing.Color.FromArgb(250, 251, 253);
        this.pnlDetails.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.pnlDetails.Controls.Add(this.txtDetails);
        this.pnlDetails.Controls.Add(this.lblDetailsTitle);
        this.pnlDetails.Dock = System.Windows.Forms.DockStyle.Fill;
        this.pnlDetails.Location = new System.Drawing.Point(0, 0);
        this.pnlDetails.Name = "pnlDetails";
        this.pnlDetails.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
        this.pnlDetails.Size = new System.Drawing.Size(876, 114);
        this.pnlDetails.TabIndex = 0;
        // 
        // lblDetailsTitle
        // 
        this.lblDetailsTitle.AutoSize = true;
        this.lblDetailsTitle.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblDetailsTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
        this.lblDetailsTitle.Location = new System.Drawing.Point(8, 4);
        this.lblDetailsTitle.Name = "lblDetailsTitle";
        this.lblDetailsTitle.Padding = new System.Windows.Forms.Padding(0, 0, 0, 4);
        this.lblDetailsTitle.Size = new System.Drawing.Size(108, 21);
        this.lblDetailsTitle.TabIndex = 0;
        this.lblDetailsTitle.Text = "Incident Details";
        // 
        // txtDetails
        // 
        this.txtDetails.BackColor = System.Drawing.Color.FromArgb(250, 251, 253);
        this.txtDetails.BorderStyle = System.Windows.Forms.BorderStyle.None;
        this.txtDetails.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtDetails.Font = new System.Drawing.Font("Consolas", 9F);
        this.txtDetails.Location = new System.Drawing.Point(8, 25);
        this.txtDetails.Multiline = true;
        this.txtDetails.Name = "txtDetails";
        this.txtDetails.ReadOnly = true;
        this.txtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtDetails.Size = new System.Drawing.Size(858, 83);
        this.txtDetails.TabIndex = 1;
        this.txtDetails.Text = "Select an incident to view details.";
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
        this.Controls.Add(this.tabMain);
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
        this.tabMain.ResumeLayout(false);
        this.tabActivityLog.ResumeLayout(false);
        this.tabIncidentHistory.ResumeLayout(false);
        this.pnlHistoryToolbar.ResumeLayout(false);
        this.pnlHistoryToolbar.PerformLayout();
        this.splitHistory.Panel1.ResumeLayout(false);
        this.splitHistory.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.splitHistory)).EndInit();
        this.splitHistory.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.dgvIncidents)).EndInit();
        this.pnlDetails.ResumeLayout(false);
        this.pnlDetails.PerformLayout();
        this.pnlSummary.ResumeLayout(false);
        this.pnlSummary.PerformLayout();
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}

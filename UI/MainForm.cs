using System.Diagnostics;
using System.ServiceProcess;
using LoginGuardService;

namespace LoginGuardUI;

public partial class MainForm : Form
{
    private readonly string _logPath = @"C:\CameraSpikeLog\service_log.txt";
    private readonly string _captureDir = @"C:\CameraSpikeLog\Captures";
    private readonly string _historyDir = @"C:\CameraSpikeLog\History";
    private long _lastLogLength = 0;
    private bool _viewManuallyCleared = false;

    // Incident history state
    private List<Incident> _allIncidents = new();
    private List<Incident> _filteredIncidents = new();
    private Incident? _selectedIncident;

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        UpdateServiceStatusHeader();
        LoadLogFile();
        SetupIncidentHistoryTab();
        refreshTimer.Start();
    }

    // ══════════════════════════════════════════════════════
    //  EXISTING: Service status header
    // ══════════════════════════════════════════════════════

    private void UpdateServiceStatusHeader()
    {
        var status = ServiceManager.GetStatus();
        if (status == null)
        {
            lblHeaderServiceStatus.Text = "● Service Not Installed";
            lblHeaderServiceStatus.ForeColor = Color.OrangeRed;
        }
        else if (status == ServiceControllerStatus.Running)
        {
            lblHeaderServiceStatus.Text = "● Service Running (Protected)";
            lblHeaderServiceStatus.ForeColor = Color.LightGreen;
        }
        else if (status == ServiceControllerStatus.Stopped)
        {
            lblHeaderServiceStatus.Text = "● Service Stopped";
            lblHeaderServiceStatus.ForeColor = Color.Salmon;
        }
        else
        {
            lblHeaderServiceStatus.Text = $"● Service: {status}";
            lblHeaderServiceStatus.ForeColor = Color.Khaki;
        }
    }

    // ══════════════════════════════════════════════════════
    //  EXISTING: Activity Log
    // ══════════════════════════════════════════════════════

    private void LoadLogFile()
    {
        if (!File.Exists(_logPath))
        {
            lblEventCount.Text = "Log file not yet created.";
            return;
        }

        try
        {
            var fileInfo = new FileInfo(_logPath);
            if (fileInfo.Length == _lastLogLength && !_viewManuallyCleared && lvwActivity.Items.Count > 0)
            {
                return; // Nothing changed
            }

            if (_viewManuallyCleared && fileInfo.Length == _lastLogLength)
            {
                return; // Keep view cleared until new log lines arrive
            }

            _viewManuallyCleared = false;
            _lastLogLength = fileInfo.Length;

            // Read safely allowing concurrent writes from the Service
            using var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            var lines = new List<string>();
            while (reader.ReadLine() is { } line)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    lines.Add(line);
                }
            }

            lvwActivity.BeginUpdate();
            lvwActivity.Items.Clear();

            // Display in reverse chronological order (newest first)
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string line = lines[i];
                var (timestamp, friendlyEvent, rawMessage, itemColor) = ParseLogLine(line);

                var item = new ListViewItem(timestamp)
                {
                    ForeColor = itemColor
                };
                item.SubItems.Add(friendlyEvent);
                item.SubItems.Add(rawMessage);

                lvwActivity.Items.Add(item);
            }

            lvwActivity.EndUpdate();
            lblEventCount.Text = $"{lines.Count} total events recorded";
        }
        catch (Exception ex)
        {
            lblStatusInfo.Text = $"Log read warning: {ex.Message}";
        }
    }

    private static (string timestamp, string friendlyEvent, string raw, Color color) ParseLogLine(string line)
    {
        string timestamp = "—";
        string raw = line;

        int colonIdx = line.IndexOf(": ");
        if (colonIdx > 0 && colonIdx < 30)
        {
            timestamp = line[..colonIdx].Trim();
            raw = line[(colonIdx + 2)..].Trim();
        }

        // Friendly plain-English translations
        string friendly;
        Color color;

        if (raw.Contains("Failed logon detected", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🔴 Someone entered the wrong password/PIN";
            color = Color.DarkRed;
        }
        else if (raw.Contains("Telegram notification sent successfully", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🟢 Telegram alert sent with photo";
            color = Color.FromArgb(16, 124, 65);
        }
        else if (raw.Contains("CAPTURE SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "📷 Webcam photo captured successfully";
            color = Color.FromArgb(0, 99, 177);
        }
        else if (raw.Contains("CAPTURE FAILURE", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "❌ Webcam capture failed";
            color = Color.Crimson;
        }
        else if (raw.Contains("Remote lock command received", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🔒 Remote lock received from Telegram";
            color = Color.DarkMagenta;
        }
        else if (raw.Contains("Laptop locked successfully", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🔒 Active laptop session successfully locked";
            color = Color.DarkMagenta;
        }
        else if (raw.Contains("Queued notification for retry", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "⏳ Offline: alert queued for retry when reconnected";
            color = Color.DarkOrange;
        }
        else if (raw.Contains("Telegram notification failed", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "⚠️ Telegram send failed (network down or offline)";
            color = Color.OrangeRed;
        }
        else if (raw.Contains("Network connectivity detected", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🌐 Network connected — immediate alert retry triggered";
            color = Color.DarkCyan;
        }
        else if (raw.Contains("Service starting", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "ℹ️ LoginGuard Service starting";
            color = Color.DarkSlateGray;
        }
        else if (raw.Contains("Watcher active. Service running", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🛡️ Watcher active — real-time monitoring running";
            color = Color.FromArgb(16, 124, 65);
        }
        else if (raw.Contains("Log rotated", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "📋 Log rotated and archived";
            color = Color.Gray;
        }
        else if (raw.Contains("Cleanup: removed", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🧹 Periodic cleanup: purged expired photos/logs";
            color = Color.DimGray;
        }
        else if (raw.Contains("USB Storage Inserted", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🔌 USB removable drive inserted";
            color = Color.DarkGoldenrod;
        }
        else if (raw.Contains("USB Telegram alert sent", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🟢 Telegram alert sent for USB storage";
            color = Color.FromArgb(16, 124, 65);
        }
        else if (raw.Contains("Shutdown detected", StringComparison.OrdinalIgnoreCase) || raw.Contains("System Shutdown", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "⚫ System shutdown detected";
            color = Color.DarkSlateBlue;
        }
        else if (raw.Contains("Restart detected", StringComparison.OrdinalIgnoreCase) || raw.Contains("System Restart", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "🔄 System restart detected";
            color = Color.DarkOrange;
        }
        else if (raw.Contains("Unexpected shutdown", StringComparison.OrdinalIgnoreCase))
        {
            friendly = "⚠️ Previous system shutdown was unexpected";
            color = Color.DarkRed;
        }
        else
        {
            friendly = $"ℹ️ {raw}";
            color = Color.Black;
        }

        return (timestamp, friendly, raw, color);
    }

    // ══════════════════════════════════════════════════════
    //  EXISTING: Activity Log toolbar
    // ══════════════════════════════════════════════════════

    private void btnOpenCaptures_Click(object? sender, EventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_captureDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = _captureDir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open captures folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnSettings_Click(object? sender, EventArgs e)
    {
        using var settings = new SettingsForm();
        settings.ShowDialog(this);
        UpdateServiceStatusHeader();
        LoadLogFile();
    }

    private void btnRefreshLog_Click(object? sender, EventArgs e)
    {
        _viewManuallyCleared = false;
        UpdateServiceStatusHeader();
        LoadLogFile();
    }

    private void btnClearView_Click(object? sender, EventArgs e)
    {
        _viewManuallyCleared = true;
        lvwActivity.Items.Clear();
        lblEventCount.Text = "View cleared (log file on disk preserved)";
    }

    private void refreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateServiceStatusHeader();
        LoadLogFile();
    }

    // ══════════════════════════════════════════════════════
    //  NEW: Incident History
    // ══════════════════════════════════════════════════════

    private void SetupIncidentHistoryTab()
    {
        // Populate filter combo
        cboHistoryFilter.Items.Clear();
        cboHistoryFilter.Items.AddRange(new object[]
        {
            "All Events",
            "Failed Logins Only",
            "USB Activity Only",
            "Shutdown / Restart Only",
            "Capture Successful",
            "Capture Failed",
            "Telegram Sent",
            "Telegram Pending/Queued",
            "Telegram Failed"
        });
        cboHistoryFilter.SelectedIndex = 0;

        // Setup DataGridView columns
        dgvIncidents.Columns.Clear();
        dgvIncidents.Columns.Add("colTime", "Time");
        dgvIncidents.Columns.Add("colEvent", "Event");
        dgvIncidents.Columns.Add("colDetails", "Details");
        dgvIncidents.Columns.Add("colCapture", "Capture");
        dgvIncidents.Columns.Add("colTelegram", "Telegram");

        dgvIncidents.Columns["colTime"]!.FillWeight = 20;
        dgvIncidents.Columns["colEvent"]!.FillWeight = 20;
        dgvIncidents.Columns["colDetails"]!.FillWeight = 30;
        dgvIncidents.Columns["colCapture"]!.FillWeight = 15;
        dgvIncidents.Columns["colTelegram"]!.FillWeight = 15;

        // Set alternating row colors for readability
        dgvIncidents.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 247, 250);

        // Load initial data
        LoadIncidentHistory();
    }

    private void LoadIncidentHistory()
    {
        try
        {
            _allIncidents = IncidentRepository.LoadAllIncidents(_historyDir);
        }
        catch (Exception)
        {
            _allIncidents = new List<Incident>();
        }

        ApplyFilterAndRefreshGrid();
    }

    private void ApplyFilterAndRefreshGrid()
    {
        // Map combo index to filter enum
        var filter = cboHistoryFilter.SelectedIndex switch
        {
            1 => IncidentHistoryHelper.IncidentFilter.FailedLoginsOnly,
            2 => IncidentHistoryHelper.IncidentFilter.UsbOnly,
            3 => IncidentHistoryHelper.IncidentFilter.ShutdownRestartOnly,
            4 => IncidentHistoryHelper.IncidentFilter.CaptureSuccessful,
            5 => IncidentHistoryHelper.IncidentFilter.CaptureFailed,
            6 => IncidentHistoryHelper.IncidentFilter.TelegramSent,
            7 => IncidentHistoryHelper.IncidentFilter.TelegramPending,
            8 => IncidentHistoryHelper.IncidentFilter.TelegramFailed,
            _ => IncidentHistoryHelper.IncidentFilter.All
        };

        _filteredIncidents = IncidentHistoryHelper.ApplyFilter(_allIncidents, filter);

        // Populate grid
        dgvIncidents.Rows.Clear();

        if (_filteredIncidents.Count == 0)
        {
            // Show a placeholder message via the summary
            txtDetails.Text = "Select an incident to view details.";
            _selectedIncident = null;
        }

        foreach (var inc in _filteredIncidents)
        {
            int rowIdx = dgvIncidents.Rows.Add(
                inc.DetectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                IncidentHistoryHelper.FormatEventType(inc.EventType),
                IncidentHistoryHelper.FormatEventDetails(inc),
                IncidentHistoryHelper.FormatCaptureStatus(inc.CaptureStatus),
                IncidentHistoryHelper.FormatNotificationStatus(inc.NotificationStatus)
            );
            dgvIncidents.Rows[rowIdx].Tag = inc;
        }

        // Update summary
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        var summary = IncidentHistoryHelper.CalculateSummary(_allIncidents);

        lblSummaryStats.Text = $"Total: {summary.TotalIncidents}  |  " +
                               $"Captures: {summary.SuccessfulCaptures}  |  " +
                               $"USB: {summary.UsbEvents}  |  " +
                               $"Sent: {summary.TelegramSent}  |  " +
                               $"Pending: {summary.TelegramPending}";
        lblSummaryLatest.Text = $"Latest: {summary.LatestIncidentInfo}";
    }

    private void dgvIncidents_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvIncidents.SelectedRows.Count == 0)
        {
            _selectedIncident = null;
            txtDetails.Text = "Select an incident to view details.";
            return;
        }

        _selectedIncident = dgvIncidents.SelectedRows[0].Tag as Incident;
        if (_selectedIncident == null)
        {
            txtDetails.Text = "Unable to load incident details.";
            return;
        }

        var inc = _selectedIncident;
        var lines = new List<string>
        {
            $"Event Type:             {IncidentHistoryHelper.FormatEventType(inc.EventType)}",
            $"Incident ID:            {inc.IncidentId}",
            $"Detected At:            {inc.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}",
        };

        if (inc.EventType == SecurityEventType.USBStorageInserted)
        {
            lines.Add($"Device Name:            {inc.DeviceName ?? "Unknown USB Device"}");
            if (!string.IsNullOrEmpty(inc.DeviceDescription))
                lines.Add($"Description:            {inc.DeviceDescription}");
            if (!string.IsNullOrEmpty(inc.DeviceClass))
                lines.Add($"Device Class / Type:    {inc.DeviceClass}");
            if (!string.IsNullOrEmpty(inc.Manufacturer))
                lines.Add($"Manufacturer:           {inc.Manufacturer}");
            if (!string.IsNullOrEmpty(inc.HardwareId))
                lines.Add($"Hardware ID:            {inc.HardwareId}");
            if (!string.IsNullOrEmpty(inc.DeviceId))
                lines.Add($"Device ID (Instance):   {inc.DeviceId}");
            if (!string.IsNullOrEmpty(inc.DriveLetter))
                lines.Add($"Drive Letter:           {inc.DriveLetter}");
            if (!string.IsNullOrEmpty(inc.VolumeLabel))
                lines.Add($"Volume Label:           {inc.VolumeLabel}");
            if (!string.IsNullOrEmpty(inc.Details))
                lines.Add($"Details:                {inc.Details}");
        }
        else if (inc.EventType is SecurityEventType.Shutdown or SecurityEventType.Restart or SecurityEventType.UnexpectedShutdown)
        {
            lines.Add($"Details:                {inc.Details ?? "(none)"}");
        }
        else
        {
            lines.Add($"Target User:            {inc.TargetUser}");
            lines.Add($"Workstation:            {inc.Workstation}");
            lines.Add($"Logon Type:             {IncidentHistoryHelper.FormatLogonType(inc.LogonType)}");
        }

        lines.Add($"Capture Status:         {IncidentHistoryHelper.FormatCaptureStatus(inc.CaptureStatus)}");
        if (!string.IsNullOrEmpty(inc.CapturePath))
        {
            lines.Add($"Capture Path:           {inc.CapturePath}");
        }

        lines.Add($"Notification Status:    {IncidentHistoryHelper.FormatNotificationStatus(inc.NotificationStatus)}");
        lines.Add($"Notification Attempts:  {inc.NotificationAttempts}");
        if (inc.LastNotificationAttempt.HasValue)
        {
            lines.Add($"Last Attempt:           {inc.LastNotificationAttempt.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        }
        if (!string.IsNullOrEmpty(inc.LastNotificationError))
        {
            lines.Add($"Last Error:             {inc.LastNotificationError}");
        }

        lines.Add($"Created At:             {inc.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        lines.Add($"Updated At:             {inc.UpdatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}");

        txtDetails.Text = string.Join(Environment.NewLine, lines);
    }

    private void btnRefreshHistory_Click(object? sender, EventArgs e)
    {
        LoadIncidentHistory();
    }

    private void cboHistoryFilter_SelectedIndexChanged(object? sender, EventArgs e)
    {
        ApplyFilterAndRefreshGrid();
    }

    private void btnOpenCapture_Click(object? sender, EventArgs e)
    {
        if (_selectedIncident == null)
        {
            MessageBox.Show("Please select an incident first.", "No Selection",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_selectedIncident.CaptureStatus == CaptureStatus.NotApplicable)
        {
            MessageBox.Show("Webcam captures are not recorded for this event type.", "No Capture",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(_selectedIncident.CapturePath))
        {
            MessageBox.Show("No capture file is associated with this incident.",
                "Capture Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!File.Exists(_selectedIncident.CapturePath))
        {
            MessageBox.Show(
                "Capture unavailable — file may have been removed by retention cleanup.\n\n" +
                $"Expected path: {_selectedIncident.CapturePath}",
                "Capture Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _selectedIncident.CapturePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open capture file: {ex.Message}",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

using System.Diagnostics;
using System.ServiceProcess;

namespace LoginGuardUI;

public partial class MainForm : Form
{
    private readonly string _logPath = @"C:\CameraSpikeLog\service_log.txt";
    private readonly string _captureDir = @"C:\CameraSpikeLog\Captures";
    private long _lastLogLength = 0;

    public MainForm()
    {
        InitializeComponent();
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        UpdateServiceStatusHeader();
        LoadLogFile();
        refreshTimer.Start();
    }

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
            if (fileInfo.Length == _lastLogLength && lvwActivity.Items.Count > 0)
            {
                return; // Nothing changed
            }

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
            friendly = "🧹 Periodic cleanup: purged expired photos";
            color = Color.DimGray;
        }
        else
        {
            friendly = $"ℹ️ {raw}";
            color = Color.Black;
        }

        return (timestamp, friendly, raw, color);
    }

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
        UpdateServiceStatusHeader();
        LoadLogFile();
    }

    private void btnClearView_Click(object? sender, EventArgs e)
    {
        lvwActivity.Items.Clear();
        lblEventCount.Text = "View cleared (log file on disk preserved)";
    }

    private void refreshTimer_Tick(object? sender, EventArgs e)
    {
        UpdateServiceStatusHeader();
        LoadLogFile();
    }
}

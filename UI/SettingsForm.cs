using System.ServiceProcess;
using OpenCvSharp;

namespace LoginGuardUI;

public partial class SettingsForm : Form
{
    private class PresetItem<T>(string text, T value)
    {
        public string Text { get; } = text;
        public T Value { get; } = value;
        public override string ToString() => Text;
    }

    private class CameraItem(int index, string description)
    {
        public int Index { get; } = index;
        public string Description { get; } = description;
        public override string ToString() => Description;
    }

    public SettingsForm()
    {
        InitializeComponent();
    }

    private void SettingsForm_Load(object? sender, EventArgs e)
    {
        PopulateRetentionPresets();
        EnumerateCameras();
        LoadCurrentConfiguration();
        RefreshServiceStatus();
    }

    private void PopulateRetentionPresets()
    {
        // Capture Retention Presets: 7, 15, 30, 60, -1 (Never)
        cboCaptureRetention.Items.Clear();
        cboCaptureRetention.Items.Add(new PresetItem<int>("7 Days", 7));
        cboCaptureRetention.Items.Add(new PresetItem<int>("15 Days", 15));
        cboCaptureRetention.Items.Add(new PresetItem<int>("30 Days (Default)", 30));
        cboCaptureRetention.Items.Add(new PresetItem<int>("60 Days", 60));
        cboCaptureRetention.Items.Add(new PresetItem<int>("Never - Keep All Captures", -1));

        // Log Retention Presets: 7, 15, 30, 90
        cboLogRetention.Items.Clear();
        cboLogRetention.Items.Add(new PresetItem<int>("7 Days", 7));
        cboLogRetention.Items.Add(new PresetItem<int>("15 Days (Default)", 15));
        cboLogRetention.Items.Add(new PresetItem<int>("30 Days", 30));
        cboLogRetention.Items.Add(new PresetItem<int>("90 Days", 90));
    }

    private void EnumerateCameras()
    {
        cboCamera.Items.Clear();
        try
        {
            for (int i = 0; i < 5; i++)
            {
                using var probe = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                if (probe.IsOpened())
                {
                    cboCamera.Items.Add(new CameraItem(i, $"Camera {i} ({probe.FrameWidth}x{probe.FrameHeight})"));
                    probe.Release();
                }
            }
        }
        catch (Exception ex)
        {
            lblCameraStatus.Text = $"Camera probe warning: {ex.Message}";
        }

        if (cboCamera.Items.Count == 0)
        {
            cboCamera.Items.Add(new CameraItem(0, "Camera 0 (Default fallback)"));
        }
    }

    private void LoadCurrentConfiguration()
    {
        var config = ConfigManager.Load();

        txtBotToken.Text = config.Telegram.BotToken;
        txtChatId.Text = config.Telegram.ChatId;

        // Select camera
        int targetCam = config.Camera.DeviceIndex;
        int selectedCamIdx = 0;
        for (int i = 0; i < cboCamera.Items.Count; i++)
        {
            if (cboCamera.Items[i] is CameraItem item && item.Index == targetCam)
            {
                selectedCamIdx = i;
                break;
            }
        }
        cboCamera.SelectedIndex = selectedCamIdx;

        // Select capture retention
        int targetCap = config.Storage.CaptureRetentionDays;
        int selectedCapIdx = 2; // default 30 days
        for (int i = 0; i < cboCaptureRetention.Items.Count; i++)
        {
            if (cboCaptureRetention.Items[i] is PresetItem<int> item && item.Value == targetCap)
            {
                selectedCapIdx = i;
                break;
            }
        }
        cboCaptureRetention.SelectedIndex = selectedCapIdx;

        // Select log retention
        int targetLog = config.Storage.LogRetentionDays;
        int selectedLogIdx = 1; // default 15 days
        for (int i = 0; i < cboLogRetention.Items.Count; i++)
        {
            if (cboLogRetention.Items[i] is PresetItem<int> item && item.Value == targetLog)
            {
                selectedLogIdx = i;
                break;
            }
        }
        cboLogRetention.SelectedIndex = selectedLogIdx;
    }

    private void chkShowToken_CheckedChanged(object? sender, EventArgs e)
    {
        txtBotToken.UseSystemPasswordChar = !chkShowToken.Checked;
    }

    private async void btnTestTelegram_Click(object? sender, EventArgs e)
    {
        string token = txtBotToken.Text.Trim();
        string chatId = txtChatId.Text.Trim();

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
        {
            MessageBox.Show("Please enter both Bot Token and Chat ID before testing.", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnTestTelegram.Enabled = false;
        lblTelegramStatus.ForeColor = System.Drawing.Color.Blue;
        lblTelegramStatus.Text = "Sending test message to Telegram...";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            string url = $"https://api.telegram.org/bot{token}/sendMessage";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("chat_id", chatId),
                new KeyValuePair<string, string>("text", $"🔒 LoginGuard: Test message sent successfully from Settings on {DateTime.Now:yyyy-MM-dd HH:mm:ss}.")
            });

            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                lblTelegramStatus.ForeColor = System.Drawing.Color.FromArgb(16, 124, 65);
                lblTelegramStatus.Text = "✅ Test message delivered successfully! Check your Telegram.";
            }
            else
            {
                string body = await response.Content.ReadAsStringAsync();
                lblTelegramStatus.ForeColor = System.Drawing.Color.Red;
                lblTelegramStatus.Text = $"❌ Telegram API error: {response.StatusCode} - {body}";
            }
        }
        catch (Exception ex)
        {
            lblTelegramStatus.ForeColor = System.Drawing.Color.Red;
            lblTelegramStatus.Text = $"❌ Network error: {ex.Message}";
        }
        finally
        {
            btnTestTelegram.Enabled = true;
        }
    }

    private async void btnTestCapture_Click(object? sender, EventArgs e)
    {
        int camIndex = 0;
        if (cboCamera.SelectedItem is CameraItem cam)
        {
            camIndex = cam.Index;
        }

        btnTestCapture.Enabled = false;
        lblCameraStatus.ForeColor = System.Drawing.Color.Blue;
        lblCameraStatus.Text = $"Opening camera {camIndex}...";

        try
        {
            Bitmap? capturedBmp = await Task.Run(() =>
            {
                using var capture = new VideoCapture(camIndex, VideoCaptureAPIs.DSHOW);
                if (!capture.IsOpened())
                {
                    return null;
                }

                using var frame = new Mat();

                // Warmup frames
                for (int i = 0; i < 5; i++)
                {
                    capture.Read(frame);
                    Thread.Sleep(30);
                }

                bool gotFrame = false;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    capture.Read(frame);
                    if (!frame.Empty())
                    {
                        gotFrame = true;
                        break;
                    }
                    Thread.Sleep(50);
                }

                if (!gotFrame)
                {
                    return null;
                }

                Cv2.ImEncode(".bmp", frame, out byte[] buf);
                using var ms = new MemoryStream(buf);
                return new Bitmap(ms);
            });

            if (capturedBmp == null)
            {
                lblCameraStatus.ForeColor = System.Drawing.Color.Red;
                lblCameraStatus.Text = $"❌ Could not capture frame from camera {camIndex}.";
                return;
            }

            var oldImage = picPreview.Image;
            picPreview.Image = capturedBmp;
            oldImage?.Dispose();

            lblCameraStatus.ForeColor = System.Drawing.Color.FromArgb(16, 124, 65);
            lblCameraStatus.Text = $"✅ Frame captured: {capturedBmp.Width}x{capturedBmp.Height} px.";
        }
        catch (Exception ex)
        {
            lblCameraStatus.ForeColor = System.Drawing.Color.Red;
            lblCameraStatus.Text = $"❌ Capture error: {ex.Message}";
        }
        finally
        {
            btnTestCapture.Enabled = true;
        }
    }

    private void RefreshServiceStatus()
    {
        var status = ServiceManager.GetStatus();

        if (status == null)
        {
            lblServiceStatus.Text = "⚠️ Service Not Installed";
            lblServiceStatus.ForeColor = System.Drawing.Color.OrangeRed;
            btnStartService.Enabled = false;
            btnStopService.Enabled = false;
            btnRestartService.Enabled = false;
        }
        else
        {
            lblServiceStatus.Text = status switch
            {
                ServiceControllerStatus.Running => "🟢 Running (Active)",
                ServiceControllerStatus.Stopped => "🔴 Stopped",
                ServiceControllerStatus.StartPending => "🟡 Starting...",
                ServiceControllerStatus.StopPending => "🟡 Stopping...",
                _ => $"Status: {status}"
            };

            lblServiceStatus.ForeColor = status switch
            {
                ServiceControllerStatus.Running => System.Drawing.Color.FromArgb(16, 124, 65),
                ServiceControllerStatus.Stopped => System.Drawing.Color.FromArgb(209, 52, 56),
                _ => System.Drawing.Color.FromArgb(100, 100, 100)
            };

            btnStartService.Enabled = status == ServiceControllerStatus.Stopped;
            btnStopService.Enabled = status == ServiceControllerStatus.Running;
            btnRestartService.Enabled = status == ServiceControllerStatus.Running;
        }
    }

    private void btnRefreshStatus_Click(object? sender, EventArgs e)
    {
        RefreshServiceStatus();
    }

    private async void btnStartService_Click(object? sender, EventArgs e)
    {
        btnStartService.Enabled = false;
        Cursor = Cursors.WaitCursor;
        try
        {
            await Task.Run(() => ServiceManager.Start());
            RefreshServiceStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start service: {ex.Message}", "Service Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshServiceStatus();
        }
    }

    private async void btnStopService_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show("Are you sure you want to stop LoginGuardService? Security monitoring will pause.", "Confirm Stop", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        btnStopService.Enabled = false;
        Cursor = Cursors.WaitCursor;
        try
        {
            await Task.Run(() => ServiceManager.Stop());
            RefreshServiceStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to stop service: {ex.Message}", "Service Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshServiceStatus();
        }
    }

    private async void btnRestartService_Click(object? sender, EventArgs e)
    {
        btnRestartService.Enabled = false;
        Cursor = Cursors.WaitCursor;
        try
        {
            await Task.Run(() => ServiceManager.Restart());
            RefreshServiceStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to restart service: {ex.Message}", "Service Control", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            RefreshServiceStatus();
        }
    }

    private async void btnSave_Click(object? sender, EventArgs e)
    {
        string token = txtBotToken.Text.Trim();
        string chatId = txtChatId.Text.Trim();

        if (string.IsNullOrWhiteSpace(token))
        {
            tabControl.SelectedTab = tabTelegram;
            txtBotToken.Focus();
            MessageBox.Show("Please provide a valid Telegram Bot Token.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!token.Contains(':') || token.Length < 20)
        {
            tabControl.SelectedTab = tabTelegram;
            txtBotToken.Focus();
            MessageBox.Show("Telegram Bot Token format appears invalid. It should follow the format '123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ'.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(chatId))
        {
            tabControl.SelectedTab = tabTelegram;
            txtChatId.Focus();
            MessageBox.Show("Please provide a valid Authorized Chat ID.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!long.TryParse(chatId, out _))
        {
            tabControl.SelectedTab = tabTelegram;
            txtChatId.Focus();
            MessageBox.Show("Telegram Chat ID must be a numeric ID (e.g. 5635942580 or -100123456789).", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        int camIndex = (cboCamera.SelectedItem is CameraItem cam) ? cam.Index : 0;
        int captureDays = (cboCaptureRetention.SelectedItem is PresetItem<int> cap) ? cap.Value : 30;
        int logDays = (cboLogRetention.SelectedItem is PresetItem<int> log) ? log.Value : 15;

        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = token, ChatId = chatId },
            Camera = new CameraConfig { DeviceIndex = camIndex },
            Storage = new StorageConfig { CaptureRetentionDays = captureDays, LogRetentionDays = logDays }
        };

        try
        {
            ConfigManager.Save(config);

            var restartResult = MessageBox.Show(
                "Settings have been saved successfully!\n\nWould you like to restart LoginGuardService now to apply these changes?",
                "Restart Service",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (restartResult == DialogResult.Yes)
            {
                Cursor = Cursors.WaitCursor;
                try
                {
                    await Task.Run(() => ServiceManager.Restart());
                    MessageBox.Show("LoginGuardService restarted successfully with new settings.", "Service Restarted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Settings were saved, but restarting the service encountered an error: {ex.Message}\nYou can restart it manually from the Service Control tab.", "Service Restart", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

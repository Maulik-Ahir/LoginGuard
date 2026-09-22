using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OpenCvSharp;

namespace LoginGuardService;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly string _logPath = @"C:\CameraSpikeLog\service_log.txt";
    private readonly string _captureDir = @"C:\CameraSpikeLog\Captures";
    private readonly string _pendingDir = @"C:\CameraSpikeLog\PendingNotifications";

    // Dynamic configuration with battle-tested fallback defaults
    private readonly int _cameraIndex = 0;
    private readonly int _captureRetentionDays = 30; // -1 represents "Never"
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1);
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _logRetentionPeriod = TimeSpan.FromDays(15);

    private readonly object _captureLock = new();
    private bool _captureInProgress = false;
    private DateTime _lastCaptureTime = DateTime.MinValue;
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(2);

    private long _lastUpdateId = 0;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    private volatile bool _forceImmediateRetry = false;

    private EventLogWatcher? _watcher;

    public Worker(ILogger<Worker> logger, IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _config = config;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(35);

        // v0.6 configuration migration with graceful fallbacks
        if (int.TryParse(_config["Camera:DeviceIndex"], out int camIdx) && camIdx >= 0)
        {
            _cameraIndex = camIdx;
        }
        else
        {
            _cameraIndex = 0;
        }

        if (int.TryParse(_config["Storage:CaptureRetentionDays"], out int capDays))
        {
            _captureRetentionDays = capDays;
        }
        else
        {
            _captureRetentionDays = 30;
        }

        if (int.TryParse(_config["Storage:LogRetentionDays"], out int logDays) && logDays > 0)
        {
            _logRetentionPeriod = TimeSpan.FromDays(logDays);
        }
        else
        {
            _logRetentionPeriod = TimeSpan.FromDays(15);
        }
    }

    private void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
            File.AppendAllText(_logPath, $"{DateTime.Now:dd-MM-yyyy HH:mm:ss}: {message}{Environment.NewLine}");
        }
        catch
        {
            // Suppress file access collisions in logging
        }
        _logger.LogInformation("{Message}", message);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Directory.CreateDirectory(_captureDir);
        Log($"Service starting. Active camera index: {_cameraIndex}, Capture retention: {(_captureRetentionDays == -1 ? "Never" : $"{_captureRetentionDays} days")}, Log retention: {_logRetentionPeriod.TotalDays} days.");

        EnsureAuditPolicyEnabled();
        LogAvailableCameras();

        NetworkChange.NetworkAvailabilityChanged += (s, e) =>
        {
            if (e.IsAvailable)
            {
                Log("Network connectivity detected — triggering immediate retry.");
                _forceImmediateRetry = true;
            }
        };

        try
        {
            string queryString = "*[System[(EventID=4625)]]";
            EventLogQuery query = new EventLogQuery("Security", PathType.LogName, queryString);

            _watcher = new EventLogWatcher(query);
            _watcher.EventRecordWritten += OnFailedLogonEvent;
            _watcher.Enabled = true;

            Log("Watcher active. Service running.");
        }
        catch (Exception ex)
        {
            Log($"FATAL: Failed to start watcher: {ex.Message}");
            return;
        }

        _ = RunPeriodicCleanupAsync(stoppingToken);
        _ = RunTelegramPollingAsync(stoppingToken);
        _ = RunPendingNotificationRetryAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Expected on shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Log("Service stopping...");
        if (_watcher != null)
        {
            _watcher.Enabled = false;
            _watcher.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }

    private void EnsureAuditPolicyEnabled()
    {
        try
        {
            var checkInfo = new ProcessStartInfo
            {
                FileName = "auditpol.exe",
                Arguments = "/get /subcategory:\"Logon\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var checkProcess = Process.Start(checkInfo);
            string output = checkProcess!.StandardOutput.ReadToEnd();
            checkProcess.WaitForExit();

            bool alreadyEnabled = output.Contains("Success and Failure") ||
                                   (output.Contains("Success") && output.Contains("Failure"));

            if (alreadyEnabled)
            {
                Log("Audit policy check: Logon auditing already enabled.");
                return;
            }

            Log("Audit policy check: not fully enabled. Attempting to enable automatically...");

            var setInfo = new ProcessStartInfo
            {
                FileName = "auditpol.exe",
                Arguments = "/set /subcategory:\"Logon\" /success:enable /failure:enable",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var setProcess = Process.Start(setInfo);
            setProcess!.WaitForExit();

            Log(setProcess.ExitCode == 0
                ? "Audit policy: successfully enabled Logon auditing."
                : "Audit policy: auditpol /set returned non-zero. Manual setup may be required.");
        }
        catch (Exception ex)
        {
            Log($"Audit policy check failed: {ex.Message}. If failed logons aren't detected, manually run: " +
                "auditpol /set /subcategory:\"Logon\" /success:enable /failure:enable");
        }
    }

    private void LogAvailableCameras()
    {
        try
        {
            Log("Camera check: probing device indices 0-4...");
            for (int i = 0; i < 5; i++)
            {
                using var probe = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
                if (probe.IsOpened())
                {
                    Log($"  - Camera index {i}: available (resolution {probe.FrameWidth}x{probe.FrameHeight})");
                    probe.Release();
                }
            }
        }
        catch (Exception ex)
        {
            Log($"Camera check failed: {ex.Message}");
        }
    }

    private async Task<bool> TrySendTelegramNotificationAsync(string photoPath, string targetUser)
    {
        try
        {
            string? token = _config["Telegram:BotToken"];
            string? chatId = _config["Telegram:ChatId"]?.Trim();

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId))
            {
                Log("Telegram notification skipped: token or chat ID not configured.");
                return true;
            }

            string url = $"https://api.telegram.org/bot{token}/sendPhoto";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId), "chat_id");
            form.Add(new StringContent($"⚠️ Failed login attempt detected (account: {targetUser}) at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"), "caption");

            byte[] photoBytes = await File.ReadAllBytesAsync(photoPath);
            var photoContent = new ByteArrayContent(photoBytes);
            photoContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(photoContent, "photo", Path.GetFileName(photoPath));

            var response = await _httpClient.PostAsync(url, form);

            if (response.IsSuccessStatusCode)
            {
                Log("Telegram notification sent successfully.");
                return true;
            }

            string responseBody = await response.Content.ReadAsStringAsync();
            Log($"Telegram notification failed: {response.StatusCode} - {responseBody}");
            return false;
        }
        catch (Exception ex)
        {
            Log($"Telegram notification exception: {ex.Message}");
            return false;
        }
    }

    private void QueuePendingNotification(string photoPath, string targetUser)
    {
        try
        {
            Directory.CreateDirectory(_pendingDir);
            string queueFile = Path.Combine(_pendingDir, $"{Path.GetFileNameWithoutExtension(photoPath)}.json");
            var record = new { PhotoPath = photoPath, TargetUser = targetUser };
            File.WriteAllText(queueFile, JsonSerializer.Serialize(record));
            Log($"Queued notification for retry: {Path.GetFileName(photoPath)}");
        }
        catch (Exception ex)
        {
            Log($"Failed to queue notification: {ex.Message}");
        }
    }

    private async Task RunPendingNotificationRetryAsync(CancellationToken stoppingToken)
    {
        TimeSpan currentDelay = _retryInterval;
        TimeSpan maxDelay = TimeSpan.FromMinutes(5);
        bool loggedThisFailureStreak = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            bool anyFailureThisRound = false;

            try
            {
                if (Directory.Exists(_pendingDir))
                {
                    var queueFiles = Directory.GetFiles(_pendingDir, "*.json");

                    foreach (string queueFile in queueFiles)
                    {
                        try
                        {
                            string json = await File.ReadAllTextAsync(queueFile, stoppingToken);
                            using var doc = JsonDocument.Parse(json);
                            string photoPath = doc.RootElement.GetProperty("PhotoPath").GetString()!;
                            string targetUser = doc.RootElement.GetProperty("TargetUser").GetString()!;

                            if (!File.Exists(photoPath))
                            {
                                Log($"Queued photo no longer exists (likely cleaned up): {Path.GetFileName(photoPath)}. Removing from queue.");
                                File.Delete(queueFile);
                                continue;
                            }

                            bool sent = await TrySendTelegramNotificationAsync(photoPath, targetUser);
                            if (sent)
                            {
                                File.Delete(queueFile);
                                Log($"Successfully sent queued notification: {Path.GetFileName(photoPath)}");
                            }
                            else
                            {
                                anyFailureThisRound = true;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Error processing queued notification {Path.GetFileName(queueFile)}: {ex.Message}");
                            anyFailureThisRound = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Pending notification retry error: {ex.Message}");
                anyFailureThisRound = true;
            }

            if (anyFailureThisRound)
            {
                if (!loggedThisFailureStreak)
                {
                    Log("Still no internet — queued alert(s) will keep waiting quietly and retry automatically.");
                    loggedThisFailureStreak = true;
                }
                currentDelay = TimeSpan.FromSeconds(Math.Min(currentDelay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }
            else
            {
                currentDelay = _retryInterval;
                loggedThisFailureStreak = false;
            }

            try
            {
                if (_forceImmediateRetry)
                {
                    _forceImmediateRetry = false;
                    currentDelay = TimeSpan.FromSeconds(1);
                }
                await Task.Delay(currentDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void CleanupOldCaptures()
    {
        try
        {
            if (_captureRetentionDays < 0)
            {
                Log("Cleanup: capture retention is set to Never (-1); skipping capture deletion.");
                return;
            }

            if (!Directory.Exists(_captureDir))
            {
                return;
            }

            var files = Directory.GetFiles(_captureDir, "*.jpg");
            DateTime cutoff = DateTime.Now - TimeSpan.FromDays(_captureRetentionDays);
            int deletedCount = 0;

            foreach (string file in files)
            {
                DateTime lastWrite = File.GetLastWriteTime(file);
                if (lastWrite < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        Log($"Cleanup: failed to delete {Path.GetFileName(file)}: {ex.Message}");
                    }
                }
            }

            if (deletedCount > 0)
            {
                Log($"Cleanup: removed {deletedCount} capture(s) older than {_captureRetentionDays} days.");
            }
            else
            {
                Log("Cleanup: no captures older than retention period found.");
            }
        }
        catch (Exception ex)
        {
            Log($"Cleanup exception: {ex.Message}");
        }
    }

    private void RotateLogIfNeeded()
    {
        try
        {
            if (!File.Exists(_logPath))
            {
                return;
            }

            DateTime logCreated = File.GetCreationTime(_logPath);

            if (DateTime.Now - logCreated >= _logRetentionPeriod)
            {
                string archivePath = Path.Combine(
                    Path.GetDirectoryName(_logPath)!,
                    $"service_log_{logCreated:yyyyMMdd}.txt.old"
                );

                File.Move(_logPath, archivePath, overwrite: true);
                Log($"Log rotated. Previous log archived as {Path.GetFileName(archivePath)}.");
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(_logPath, $"{DateTime.Now:dd-MM-yyyy HH:mm:ss}: Log rotation failed: {ex.Message}{Environment.NewLine}");
            }
            catch { /* nothing more we can do */ }
        }
    }

    private async Task RunPeriodicCleanupAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            CleanupOldCaptures();
            RotateLogIfNeeded();

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private void CaptureFrame(string targetUser = "unknown")
    {
        try
        {
            using var capture = new VideoCapture(_cameraIndex, VideoCaptureAPIs.DSHOW);

            if (!capture.IsOpened())
            {
                Log($"CAPTURE FAILURE: Could not open camera (index {_cameraIndex}).");
                return;
            }

            using var frame = new Mat();

            bool gotFrame = false;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                capture.Read(frame);
                if (!frame.Empty())
                {
                    gotFrame = true;
                    break;
                }
                Thread.Sleep(1);
            }

            if (!gotFrame)
            {
                Log("CAPTURE FAILURE: Timed out waiting for frame.");
                return;
            }

            string safeUser = string.Join("_", targetUser.Split(Path.GetInvalidFileNameChars()));
            string filename = Path.Combine(_captureDir, $"capture_{DateTime.Now:yyyyMMdd_HHmmss}_{safeUser}.jpg");

            frame.SaveImage(filename);
            Log($"CAPTURE SUCCESS: Saved {filename}");

            _ = Task.Run(async () =>
            {
                bool sent = await TrySendTelegramNotificationAsync(filename, targetUser);
                if (!sent)
                {
                    QueuePendingNotification(filename, targetUser);
                }
            });
        }
        catch (Exception ex)
        {
            Log($"CAPTURE EXCEPTION: {ex.Message}");
        }
    }

    private void OnFailedLogonEvent(object? sender, EventRecordWrittenEventArgs e)
    {
        if (e.EventException != null)
        {
            Log($"EventLogWatcher error: {e.EventException.Message}");
            return;
        }

        lock (_captureLock)
        {
            if (_captureInProgress)
            {
                Log("Capture already in progress — skipping this trigger.");
                return;
            }

            if (DateTime.Now - _lastCaptureTime < _cooldown)
            {
                Log($"Trigger within cooldown window ({_cooldown.TotalSeconds}s) — skipping this trigger.");
                return;
            }

            _captureInProgress = true;
        }

        try
        {
            string targetUser = GetEventDataValue(e.EventRecord, "TargetUserName") ?? "unknown";
            string workstation = GetEventDataValue(e.EventRecord, "WorkstationName") ?? "unknown";

            Log($"Failed logon detected. Target account: {targetUser} (Workstation: {workstation}). Triggering capture...");
            CaptureFrame(targetUser);
        }
        catch (Exception ex)
        {
            Log($"Error handling event: {ex.Message}");
        }
        finally
        {
            lock (_captureLock)
            {
                _captureInProgress = false;
                _lastCaptureTime = DateTime.Now;
            }
        }
    }

    private async Task RunTelegramPollingAsync(CancellationToken stoppingToken)
    {
        string? token = _config["Telegram:BotToken"]?.Trim();
        string? allowedChatId = _config["Telegram:ChatId"]?.Trim();

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(allowedChatId))
        {
            Log("Remote lock polling skipped: Telegram not configured.");
            return;
        }

        Log("Remote lock polling started.");

        int consecutiveFailures = 0;
        TimeSpan currentDelay = _pollInterval;
        TimeSpan maxDelay = TimeSpan.FromSeconds(60);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string url = $"https://api.telegram.org/bot{token}/getUpdates?offset={_lastUpdateId + 1}&timeout=25";
                var response = await _httpClient.GetAsync(url, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    if (consecutiveFailures > 0)
                    {
                        Log("Connection restored — remote lock listening resumed normally.");
                    }
                    consecutiveFailures = 0;
                    currentDelay = _pollInterval;

                    string json = await response.Content.ReadAsStringAsync(stoppingToken);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("result", out var results))
                    {
                        foreach (var update in results.EnumerateArray())
                        {
                            long updateId = update.GetProperty("update_id").GetInt64();
                            _lastUpdateId = Math.Max(_lastUpdateId, updateId);

                            if (!update.TryGetProperty("message", out var message))
                                continue;

                            string senderChatId = message.GetProperty("chat").GetProperty("id").GetInt64().ToString();

                            if (senderChatId != allowedChatId)
                            {
                                Log($"Ignored command from unauthorized chat ID: {senderChatId}");
                                continue;
                            }

                            if (!message.TryGetProperty("text", out var textElement))
                                continue;

                            string text = textElement.GetString()?.Trim().ToLowerInvariant() ?? "";

                            if (text == "/lock")
                            {
                                Log("Remote lock command received. Locking active session now.");
                                bool locked = LockActiveSessionAsUser();
                                await SendSimpleTelegramMessageAsync(locked
                                    ? "🔒 Laptop locked successfully."
                                    : "⚠️ Lock command received but failed — check service_log.txt.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                consecutiveFailures++;
                if (consecutiveFailures == 1)
                {
                    Log($"Telegram polling error: {ex.Message} (consecutive failures: {consecutiveFailures})");
                }

                currentDelay = TimeSpan.FromSeconds(Math.Min(currentDelay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }

            try
            {
                if (_forceImmediateRetry)
                {
                    _forceImmediateRetry = false;
                    currentDelay = TimeSpan.FromSeconds(1);
                }
                await Task.Delay(currentDelay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task SendSimpleTelegramMessageAsync(string text)
    {
        try
        {
            string? token = _config["Telegram:BotToken"]?.Trim();
            string? chatId = _config["Telegram:ChatId"]?.Trim();
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(chatId)) return;

            string url = $"https://api.telegram.org/bot{token}/sendMessage";
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("chat_id", chatId),
                new KeyValuePair<string, string>("text", text)
            });

            await _httpClient.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            Log($"Failed to send confirmation message: {ex.Message}");
        }
    }

    private string? GetEventDataValue(EventRecord record, string dataName)
    {
        try
        {
            var xml = new System.Xml.XmlDocument();
            xml.LoadXml(record.ToXml());

            var nsmgr = new System.Xml.XmlNamespaceManager(xml.NameTable);
            nsmgr.AddNamespace("ns", "http://schemas.microsoft.com/win/2004/08/events/event");

            var node = xml.SelectSingleNode($"//ns:Data[@Name='{dataName}']", nsmgr);
            return node?.InnerText;
        }
        catch
        {
            return null;
        }
    }

    // ---------- Session-aware remote lock (Session 0 Breakout) ----------
    [DllImport("kernel32.dll")]
    private static extern uint WTSGetActiveConsoleSessionId();

    [DllImport("wtsapi32.dll", SetLastError = true)]
    private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess,
        IntPtr lpTokenAttributes, int impersonationLevel, int tokenType, out IntPtr phNewToken);

    [DllImport("userenv.dll", SetLastError = true)]
    private static extern bool CreateEnvironmentBlock(out IntPtr lpEnvironment, IntPtr hToken, bool bInherit);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern bool CreateProcessAsUser(IntPtr hToken, string? lpApplicationName, System.Text.StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
        IntPtr lpEnvironment, string? lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }

    private bool LockActiveSessionAsUser()
    {
        IntPtr userToken = IntPtr.Zero;
        IntPtr dupToken = IntPtr.Zero;

        try
        {
            uint sessionId = WTSGetActiveConsoleSessionId();

            if (!WTSQueryUserToken(sessionId, out userToken))
            {
                Log($"LockActiveSessionAsUser: WTSQueryUserToken failed (session {sessionId}). Win32 error: {Marshal.GetLastWin32Error()}");
                return false;
            }

            if (!DuplicateTokenEx(userToken, 0x10000000, IntPtr.Zero,
                    2, 1, out dupToken))
            {
                Log($"LockActiveSessionAsUser: DuplicateTokenEx failed. Win32 error: {Marshal.GetLastWin32Error()}");
                return false;
            }

            CreateEnvironmentBlock(out IntPtr envBlock, dupToken, false);

            var startupInfo = new STARTUPINFO();
            startupInfo.cb = Marshal.SizeOf(startupInfo);
            startupInfo.lpDesktop = "winsta0\\default";

            var cmd = new System.Text.StringBuilder(@"C:\Windows\System32\rundll32.exe user32.dll,LockWorkStation");

            bool success = CreateProcessAsUser(dupToken, null, cmd, IntPtr.Zero, IntPtr.Zero, false,
                0x00000400, envBlock, null, ref startupInfo, out PROCESS_INFORMATION procInfo);

            if (!success)
            {
                Log($"LockActiveSessionAsUser: CreateProcessAsUser failed. Win32 error: {Marshal.GetLastWin32Error()}");
            }
            else
            {
                CloseHandle(procInfo.hProcess);
                CloseHandle(procInfo.hThread);
            }

            return success;
        }
        catch (Exception ex)
        {
            Log($"LockActiveSessionAsUser exception: {ex.Message}");
            return false;
        }
        finally
        {
            if (dupToken != IntPtr.Zero) CloseHandle(dupToken);
            if (userToken != IntPtr.Zero) CloseHandle(userToken);
        }
    }
}

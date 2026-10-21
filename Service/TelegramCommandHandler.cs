using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace LoginGuardService;

/// <summary>
/// Testable Telegram command handler. Formats responses for all commands.
/// Actual Telegram API calls remain in Worker.cs.
/// </summary>
public static class TelegramCommandHandler
{
    // ── Command parsing ────────────────────────────────────

    public static string ParseCommand(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return "";

        string trimmed = rawText.Trim().ToLowerInvariant();

        // Handle bot-mention suffix like "/lock@MyBot"
        int atIdx = trimmed.IndexOf('@');
        if (atIdx > 0)
            trimmed = trimmed[..atIdx];

        return trimmed;
    }

    public static bool IsKnownCommand(string command)
    {
        return command is "/help" or "/status" or "/last" or "/history" or "/pending" or "/lock" or "/usb";
    }

    // ── /help ──────────────────────────────────────────────

    public static string FormatHelp()
    {
        return
            "📋 LoginGuard Commands\n\n" +
            "/status — current service status\n" +
            "/last — latest incident\n" +
            "/history — recent incidents\n" +
            "/pending — pending notifications\n" +
            "/usb — recent USB activity\n" +
            "/lock — lock the active Windows session\n" +
            "/help — show available commands";
    }

    // ── /status ────────────────────────────────────────────

    public static string FormatStatus(int cameraIndex, int pendingAlertCount, string? lastIncidentTime, string? lastNotificationStatus)
    {
        bool networkOnline;
        try
        {
            networkOnline = NetworkInterface.GetIsNetworkAvailable();
        }
        catch
        {
            networkOnline = false;
        }

        return
            "📊 LoginGuard Status\n\n" +
            $"Service: Running\n" +
            $"Camera: Configured (Index {cameraIndex})\n" +
            $"Network: {(networkOnline ? "Online" : "Offline")}\n" +
            $"Pending Alerts: {pendingAlertCount}\n" +
            $"Last Incident: {lastIncidentTime ?? "None"}\n" +
            $"Last Notification: {lastNotificationStatus ?? "None"}";
    }

    // ── /last ──────────────────────────────────────────────

    public class LastIncidentResult
    {
        public string Message { get; init; } = "";
        public string? CaptureFilePath { get; init; }
    }

    public static LastIncidentResult FormatLastIncident(string historyDir)
    {
        try
        {
            var incidents = IncidentRepository.LoadAllIncidents(historyDir);
            if (incidents.Count == 0)
            {
                return new LastIncidentResult { Message = "No incidents recorded yet." };
            }

            var latest = incidents[0];
            string captureStatusText = FormatCaptureStatusEmoji(latest.CaptureStatus);
            string notifStatusText = FormatNotificationStatusEmoji(latest.NotificationStatus);

            string message;
            if (latest.EventType == SecurityEventType.USBStorageInserted)
            {
                var vidPid = UsbStorageDetector.ExtractVidPid(latest.HardwareId ?? latest.DeviceId);
                message =
                    "🔍 Latest Incident: USB Device Connected\n\n" +
                    $"Time: {latest.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n" +
                    $"Device: {latest.DeviceName ?? "USB Device"}\n" +
                    $"Type: {latest.DeviceClass ?? "device"}\n" +
                    (!string.IsNullOrWhiteSpace(latest.Manufacturer) ? $"Manufacturer: {latest.Manufacturer}\n" : "") +
                    (!string.IsNullOrWhiteSpace(latest.DriveLetter) ? $"Drive: {latest.DriveLetter}\n" : "") +
                    (!string.IsNullOrWhiteSpace(vidPid) ? $"Hardware ID: {vidPid}\n" : "") +
                    (!string.IsNullOrWhiteSpace(latest.VolumeLabel) ? $"Volume: {latest.VolumeLabel}\n" : "") +
                    $"Telegram: {notifStatusText}";
            }
            else if (latest.EventType is SecurityEventType.Shutdown or SecurityEventType.Restart or SecurityEventType.UnexpectedShutdown)
            {
                message =
                    $"🔍 Latest Incident: {latest.EventType}\n\n" +
                    $"Time: {latest.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n" +
                    $"Details: {latest.Details ?? "(none)"}\n" +
                    $"Telegram: {notifStatusText}";
            }
            else
            {
                message =
                    "🔍 Latest Incident\n\n" +
                    $"Time: {latest.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}\n" +
                    $"Account: {latest.TargetUser}\n" +
                    $"Logon Type: {FormatLogonType(latest.LogonType)}\n" +
                    $"Workstation: {latest.Workstation}\n" +
                    $"Capture: {captureStatusText}\n" +
                    $"Telegram: {notifStatusText}";
            }

            string? capturePath = null;
            if (latest.CaptureStatus == CaptureStatus.Success &&
                !string.IsNullOrEmpty(latest.CapturePath) &&
                File.Exists(latest.CapturePath))
            {
                capturePath = latest.CapturePath;
            }
            else if (latest.CaptureStatus == CaptureStatus.Success &&
                     !string.IsNullOrEmpty(latest.CapturePath) &&
                     !File.Exists(latest.CapturePath))
            {
                message += "\n\n⚠️ Capture file unavailable — may have been removed by retention cleanup.";
            }

            return new LastIncidentResult { Message = message, CaptureFilePath = capturePath };
        }
        catch
        {
            return new LastIncidentResult { Message = "⚠️ Error reading incident history." };
        }
    }

    // ── /history ───────────────────────────────────────────

    public static string FormatHistory(string historyDir, int maxCount = 5)
    {
        try
        {
            var incidents = IncidentRepository.LoadAllIncidents(historyDir);
            if (incidents.Count == 0)
            {
                return "No incidents recorded yet.";
            }

            var lines = new List<string> { "📜 Recent Incidents\n" };

            int count = Math.Min(incidents.Count, maxCount);
            for (int i = 0; i < count; i++)
            {
                var inc = incidents[i];
                string time = inc.DetectedAt.ToLocalTime().ToString("HH:mm");
                string notifEmoji = inc.NotificationStatus == NotificationStatus.Sent ? "✅" :
                                    inc.NotificationStatus == NotificationStatus.PermanentFailure ? "❌" :
                                    inc.NotificationStatus == NotificationStatus.NotAttempted ? "—" : "⏳";

                if (inc.EventType == SecurityEventType.USBStorageInserted)
                {
                    string dev = !string.IsNullOrWhiteSpace(inc.VolumeLabel)
                        ? inc.VolumeLabel
                        : (!string.IsNullOrWhiteSpace(inc.DeviceName) ? inc.DeviceName : "USB Device");
                    string classInfo = !string.IsNullOrWhiteSpace(inc.DeviceClass) ? $" [{inc.DeviceClass}]" : "";
                    string drive = !string.IsNullOrWhiteSpace(inc.DriveLetter) ? $" ({inc.DriveLetter})" : "";
                    lines.Add($"{i + 1}. {time} — 🔌 USB: {dev}{classInfo}{drive} — Telegram {notifEmoji}");
                }
                else if (inc.EventType == SecurityEventType.Shutdown)
                {
                    lines.Add($"{i + 1}. {time} — ⚫ Shutdown — Telegram {notifEmoji}");
                }
                else if (inc.EventType == SecurityEventType.Restart)
                {
                    lines.Add($"{i + 1}. {time} — 🔄 Restart — Telegram {notifEmoji}");
                }
                else if (inc.EventType == SecurityEventType.UnexpectedShutdown)
                {
                    lines.Add($"{i + 1}. {time} — ⚠️ Unexpected Shutdown — Telegram {notifEmoji}");
                }
                else if (inc.EventType == SecurityEventType.RemoteLock)
                {
                    lines.Add($"{i + 1}. {time} — 🔒 Remote Lock — Telegram {notifEmoji}");
                }
                else
                {
                    string capEmoji = inc.CaptureStatus == CaptureStatus.Success ? "✅" :
                                      inc.CaptureStatus == CaptureStatus.Failed ? "❌" : "⏳";
                    lines.Add($"{i + 1}. {time} — {inc.TargetUser} — {FormatLogonType(inc.LogonType)} — 📷 {capEmoji} — Telegram {notifEmoji}");
                }
            }

            if (incidents.Count > maxCount)
            {
                lines.Add($"\n({incidents.Count - maxCount} more incidents not shown)");
            }

            return string.Join("\n", lines);
        }
        catch
        {
            return "⚠️ Error reading incident history.";
        }
    }

    // ── /usb ───────────────────────────────────────────────

    public static string FormatRecentUsbActivity(string historyDir, int maxCount = 5)
    {
        try
        {
            var incidents = IncidentRepository.LoadAllIncidents(historyDir);
            var usbEvents = incidents.Where(i => i.EventType == SecurityEventType.USBStorageInserted).ToList();

            if (usbEvents.Count == 0)
            {
                return "No USB activity recorded.";
            }

            var lines = new List<string> { "Recent USB Activity\n" };
            int count = Math.Min(usbEvents.Count, maxCount);

            for (int i = 0; i < count; i++)
            {
                var ev = usbEvents[i];
                string time = ev.DetectedAt.ToLocalTime().ToString("HH:mm");
                string dev = !string.IsNullOrWhiteSpace(ev.VolumeLabel)
                    ? ev.VolumeLabel
                    : (!string.IsNullOrWhiteSpace(ev.DeviceName) ? ev.DeviceName : "USB Device");
                string classInfo = !string.IsNullOrWhiteSpace(ev.DeviceClass) ? $" [{ev.DeviceClass}]" : "";
                string drive = !string.IsNullOrWhiteSpace(ev.DriveLetter) ? $" — {ev.DriveLetter}" : "";

                lines.Add($"{i + 1}. {time} — {dev}{classInfo}{drive}");
            }

            return string.Join("\n", lines);
        }
        catch
        {
            return "⚠️ Error reading USB activity.";
        }
    }

    public static string FormatUsbAlert(Incident incident)
    {
        string device = !string.IsNullOrWhiteSpace(incident.VolumeLabel)
            ? incident.VolumeLabel
            : (!string.IsNullOrWhiteSpace(incident.DeviceName) ? incident.DeviceName : "USB Device");

        string type = !string.IsNullOrWhiteSpace(incident.DeviceClass) ? incident.DeviceClass : "device";
        string time = incident.DetectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        string header = string.Equals(incident.DeviceClass, "USB storage", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(incident.DriveLetter)
            ? "🔌 USB Storage Detected\n"
            : "🔌 USB Device Connected\n";

        var lines = new List<string>
        {
            header,
            $"Device: {device}",
            $"Type: {type}"
        };

        if (!string.IsNullOrWhiteSpace(incident.Manufacturer))
            lines.Add($"Manufacturer: {incident.Manufacturer}");

        if (!string.IsNullOrWhiteSpace(incident.DriveLetter))
            lines.Add($"Drive: {incident.DriveLetter}");

        var vidPid = UsbStorageDetector.ExtractVidPid(incident.HardwareId ?? incident.DeviceId);
        if (!string.IsNullOrWhiteSpace(vidPid))
            lines.Add($"Hardware ID: {vidPid}");

        lines.Add($"Time: {time}");

        return string.Join("\n", lines);
    }

    // ── /pending ───────────────────────────────────────────

    public static string FormatPending(string pendingDir)
    {
        try
        {
            if (!Directory.Exists(pendingDir))
            {
                return "No pending notifications.";
            }

            var files = Directory.GetFiles(pendingDir, "*.json");
            if (files.Length == 0)
            {
                return "No pending notifications.";
            }

            // Get file timestamps to determine oldest/newest
            var timestamps = new List<DateTime>();
            foreach (var file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("QueuedAt", out var queuedProp) &&
                        queuedProp.ValueKind == JsonValueKind.String)
                    {
                        if (DateTime.TryParse(queuedProp.GetString(), out var dt))
                        {
                            timestamps.Add(dt);
                            continue;
                        }
                    }
                    // Fall back to file write time
                    timestamps.Add(File.GetLastWriteTimeUtc(file));
                }
                catch
                {
                    timestamps.Add(File.GetLastWriteTimeUtc(file));
                }
            }

            timestamps.Sort();
            var oldest = timestamps[0];
            var newest = timestamps[^1];

            string oldestAgo = FormatTimeAgo(DateTime.UtcNow - oldest);
            string newestAgo = FormatTimeAgo(DateTime.UtcNow - newest);

            return
                "⏳ Pending Notifications\n\n" +
                $"Queued: {files.Length}\n" +
                $"Oldest: {oldestAgo}\n" +
                $"Newest: {newestAgo}";
        }
        catch
        {
            return "⚠️ Error reading pending notifications.";
        }
    }

    public static string FormatUnknownCommand()
    {
        return "Unknown command. Use /help.";
    }

    // ── Helper formatting ──────────────────────────────────

    private static string FormatLogonType(int logonType)
    {
        return logonType switch
        {
            2  => "Interactive (2)",
            3  => "Network (3)",
            4  => "Batch (4)",
            5  => "Service (5)",
            7  => "Unlock (7)",
            8  => "NetworkCleartext (8)",
            9  => "NewCredentials (9)",
            10 => "RemoteInteractive (10)",
            11 => "CachedInteractive (11)",
            _  => $"Type {logonType}"
        };
    }

    private static string FormatCaptureStatusEmoji(CaptureStatus status) => status switch
    {
        CaptureStatus.Success => "✅ Success",
        CaptureStatus.Failed  => "❌ Failed",
        CaptureStatus.Pending => "⏳ Pending",
        _                     => status.ToString()
    };

    private static string FormatNotificationStatusEmoji(NotificationStatus status) => status switch
    {
        NotificationStatus.Sent             => "✅ Sent",
        NotificationStatus.Queued           => "⏳ Queued",
        NotificationStatus.Retrying         => "⏳ Retrying",
        NotificationStatus.PermanentFailure => "❌ Failed",
        NotificationStatus.NotAttempted     => "— Not Attempted",
        _                                   => status.ToString()
    };

    private static string FormatTimeAgo(TimeSpan span)
    {
        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} minute{((int)span.TotalMinutes == 1 ? "" : "s")} ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} hour{((int)span.TotalHours == 1 ? "" : "s")} ago";
        return $"{(int)span.TotalDays} day{((int)span.TotalDays == 1 ? "" : "s")} ago";
    }
}

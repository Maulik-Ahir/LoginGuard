using System;
using System.Collections.Generic;
using System.Linq;
using LoginGuardService;

namespace LoginGuardUI;

/// <summary>
/// Pure helper methods for Incident History UI — formatting, filtering, summary.
/// Designed to be unit-testable without WinForms dependencies.
/// </summary>
public static class IncidentHistoryHelper
{
    // ── Logon type friendly names ──────────────────────────

    private static readonly Dictionary<int, string> LogonTypeNames = new()
    {
        { 2,  "Interactive" },
        { 3,  "Network" },
        { 4,  "Batch" },
        { 5,  "Service" },
        { 7,  "Unlock" },
        { 8,  "NetworkCleartext" },
        { 9,  "NewCredentials" },
        { 10, "RemoteInteractive" },
        { 11, "CachedInteractive" },
    };

    public static string FormatLogonType(int logonType)
    {
        return LogonTypeNames.TryGetValue(logonType, out var name)
            ? $"{name} ({logonType})"
            : $"Type {logonType}";
    }

    // ── Event Type formatting ──────────────────────────────

    public static string FormatEventType(SecurityEventType eventType) => eventType switch
    {
        SecurityEventType.FailedLogin         => "🔴 Failed Login",
        SecurityEventType.USBStorageInserted  => "🔌 USB Inserted",
        SecurityEventType.Shutdown            => "⚫ Shutdown",
        SecurityEventType.Restart             => "🔄 Restart",
        SecurityEventType.UnexpectedShutdown  => "⚠️ Unexpected Shutdown",
        SecurityEventType.RemoteLock          => "🔒 Remote Lock",
        _                                     => eventType.ToString()
    };

    public static string FormatEventDetails(Incident incident) => incident.EventType switch
    {
        SecurityEventType.USBStorageInserted =>
            !string.IsNullOrWhiteSpace(incident.DeviceName)
                ? $"{incident.DeviceName} [{incident.DeviceClass ?? "USB"}]{(!string.IsNullOrWhiteSpace(incident.DriveLetter) ? $" ({incident.DriveLetter})" : "")}"
                : (!string.IsNullOrWhiteSpace(incident.VolumeLabel) ? $"{incident.VolumeLabel} ({incident.DriveLetter})" : "USB Device"),

        SecurityEventType.Shutdown           => incident.Details ?? "Normal Shutdown",
        SecurityEventType.Restart            => incident.Details ?? "System Restart",
        SecurityEventType.UnexpectedShutdown => incident.Details ?? "Unexpected Power Loss / Crash",
        SecurityEventType.RemoteLock         => "Remote Lock via Telegram",
        _                                    => $"{incident.TargetUser} — {FormatLogonType(incident.LogonType)}"
    };

    // ── Status formatting ──────────────────────────────────

    public static string FormatCaptureStatus(CaptureStatus status) => status switch
    {
        CaptureStatus.Success       => "✅ Success",
        CaptureStatus.Failed        => "❌ Failed",
        CaptureStatus.Pending       => "⏳ Pending",
        CaptureStatus.NotApplicable => "— N/A",
        _                           => status.ToString()
    };

    public static string FormatNotificationStatus(NotificationStatus status) => status switch
    {
        NotificationStatus.Sent             => "✅ Sent",
        NotificationStatus.Queued           => "⏳ Queued",
        NotificationStatus.Retrying         => "⏳ Retrying",
        NotificationStatus.PermanentFailure => "❌ Failed",
        NotificationStatus.NotAttempted     => "— Not Attempted",
        _                                   => status.ToString()
    };

    // ── Filtering ──────────────────────────────────────────

    public enum IncidentFilter
    {
        All,
        FailedLoginsOnly,
        UsbOnly,
        ShutdownRestartOnly,
        CaptureSuccessful,
        CaptureFailed,
        TelegramSent,
        TelegramPending,
        TelegramFailed
    }

    public static List<Incident> ApplyFilter(List<Incident> incidents, IncidentFilter filter)
    {
        return filter switch
        {
            IncidentFilter.All                 => incidents,
            IncidentFilter.FailedLoginsOnly    => incidents.Where(i => i.EventType == SecurityEventType.FailedLogin).ToList(),
            IncidentFilter.UsbOnly             => incidents.Where(i => i.EventType == SecurityEventType.USBStorageInserted).ToList(),
            IncidentFilter.ShutdownRestartOnly => incidents.Where(i => i.EventType is SecurityEventType.Shutdown
                                                                                   or SecurityEventType.Restart
                                                                                   or SecurityEventType.UnexpectedShutdown).ToList(),
            IncidentFilter.CaptureSuccessful   => incidents.Where(i => i.CaptureStatus == CaptureStatus.Success).ToList(),
            IncidentFilter.CaptureFailed       => incidents.Where(i => i.CaptureStatus == CaptureStatus.Failed).ToList(),
            IncidentFilter.TelegramSent        => incidents.Where(i => i.NotificationStatus == NotificationStatus.Sent).ToList(),
            IncidentFilter.TelegramPending     => incidents.Where(i => i.NotificationStatus == NotificationStatus.Queued
                                                                      || i.NotificationStatus == NotificationStatus.Retrying).ToList(),
            IncidentFilter.TelegramFailed      => incidents.Where(i => i.NotificationStatus == NotificationStatus.PermanentFailure).ToList(),
            _                                  => incidents
        };
    }

    // ── Summary ────────────────────────────────────────────

    public class IncidentSummary
    {
        public int TotalIncidents { get; init; }
        public int SuccessfulCaptures { get; init; }
        public int TelegramSent { get; init; }
        public int TelegramPending { get; init; }
        public int UsbEvents { get; init; }
        public string LatestIncidentInfo { get; init; } = "No incidents recorded";
    }

    public static IncidentSummary CalculateSummary(List<Incident> incidents)
    {
        if (incidents.Count == 0)
        {
            return new IncidentSummary();
        }

        var latest = incidents[0]; // Already sorted descending by DetectedAt
        string eventDesc = latest.EventType switch
        {
            SecurityEventType.USBStorageInserted => $"USB: {latest.DeviceName ?? latest.DriveLetter ?? "Drive"}",
            SecurityEventType.Shutdown           => "Shutdown",
            SecurityEventType.Restart            => "Restart",
            SecurityEventType.UnexpectedShutdown => "Unexpected Shutdown",
            SecurityEventType.RemoteLock         => "Remote Lock",
            _                                    => $"{latest.TargetUser} ({FormatLogonType(latest.LogonType)})"
        };
        string latestInfo = $"{latest.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm} — {eventDesc}";

        return new IncidentSummary
        {
            TotalIncidents     = incidents.Count,
            SuccessfulCaptures = incidents.Count(i => i.CaptureStatus == CaptureStatus.Success),
            TelegramSent       = incidents.Count(i => i.NotificationStatus == NotificationStatus.Sent),
            TelegramPending    = incidents.Count(i => i.NotificationStatus == NotificationStatus.Queued
                                                    || i.NotificationStatus == NotificationStatus.Retrying),
            UsbEvents          = incidents.Count(i => i.EventType == SecurityEventType.USBStorageInserted),
            LatestIncidentInfo = latestInfo
        };
    }
}

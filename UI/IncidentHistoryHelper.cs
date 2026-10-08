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

    // ── Status formatting ──────────────────────────────────

    public static string FormatCaptureStatus(CaptureStatus status) => status switch
    {
        CaptureStatus.Success => "✅ Success",
        CaptureStatus.Failed  => "❌ Failed",
        CaptureStatus.Pending => "⏳ Pending",
        _                     => status.ToString()
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
            IncidentFilter.All               => incidents,
            IncidentFilter.CaptureSuccessful  => incidents.Where(i => i.CaptureStatus == CaptureStatus.Success).ToList(),
            IncidentFilter.CaptureFailed      => incidents.Where(i => i.CaptureStatus == CaptureStatus.Failed).ToList(),
            IncidentFilter.TelegramSent       => incidents.Where(i => i.NotificationStatus == NotificationStatus.Sent).ToList(),
            IncidentFilter.TelegramPending    => incidents.Where(i => i.NotificationStatus == NotificationStatus.Queued
                                                                   || i.NotificationStatus == NotificationStatus.Retrying).ToList(),
            IncidentFilter.TelegramFailed     => incidents.Where(i => i.NotificationStatus == NotificationStatus.PermanentFailure).ToList(),
            _                                 => incidents
        };
    }

    // ── Summary ────────────────────────────────────────────

    public class IncidentSummary
    {
        public int TotalIncidents { get; init; }
        public int SuccessfulCaptures { get; init; }
        public int TelegramSent { get; init; }
        public int TelegramPending { get; init; }
        public string LatestIncidentInfo { get; init; } = "No incidents recorded";
    }

    public static IncidentSummary CalculateSummary(List<Incident> incidents)
    {
        if (incidents.Count == 0)
        {
            return new IncidentSummary();
        }

        var latest = incidents[0]; // Already sorted descending by DetectedAt
        string latestInfo = $"{latest.DetectedAt.ToLocalTime():yyyy-MM-dd HH:mm} — {latest.TargetUser} ({FormatLogonType(latest.LogonType)})";

        return new IncidentSummary
        {
            TotalIncidents   = incidents.Count,
            SuccessfulCaptures = incidents.Count(i => i.CaptureStatus == CaptureStatus.Success),
            TelegramSent     = incidents.Count(i => i.NotificationStatus == NotificationStatus.Sent),
            TelegramPending  = incidents.Count(i => i.NotificationStatus == NotificationStatus.Queued
                                                  || i.NotificationStatus == NotificationStatus.Retrying),
            LatestIncidentInfo = latestInfo
        };
    }
}

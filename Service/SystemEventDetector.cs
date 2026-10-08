using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;

namespace LoginGuardService;

public record SystemShutdownEvent(
    long? RecordId,
    DateTime Timestamp,
    int EventId,
    SecurityEventType EventType,
    string? InitiatingUser,
    string? Process,
    string Details
);

/// <summary>
/// Detects and classifies Windows system shutdown and restart events (Event ID 1074, 6008, 41)
/// using startup analysis and event logs.
/// </summary>
public class SystemEventDetector
{
    /// <summary>
    /// Pure classification logic for Windows system events.
    /// </summary>
    public static (SecurityEventType EventType, string Details) ClassifyEvent(
        int eventId,
        string? shutdownType,
        string? process,
        string? user,
        string? reason)
    {
        if (eventId == 6008 || eventId == 41)
        {
            return (SecurityEventType.UnexpectedShutdown, "Previous system shutdown was unexpected (crash or power loss)");
        }

        if (eventId == 1074)
        {
            bool isRestart = !string.IsNullOrEmpty(shutdownType) &&
                             (shutdownType.Contains("restart", StringComparison.OrdinalIgnoreCase) ||
                              shutdownType.Contains("reboot", StringComparison.OrdinalIgnoreCase));

            var type = isRestart ? SecurityEventType.Restart : SecurityEventType.Shutdown;
            string typeName = isRestart ? "Restart" : "Normal Shutdown";

            var detailsParts = new List<string> { $"Type: {typeName}" };
            if (!string.IsNullOrWhiteSpace(user))
                detailsParts.Add($"Initiated by: {user}");
            if (!string.IsNullOrWhiteSpace(process))
                detailsParts.Add($"Process: {Path.GetFileName(process)}");
            if (!string.IsNullOrWhiteSpace(reason))
                detailsParts.Add($"Reason: {reason}");

            return (type, string.Join(" | ", detailsParts));
        }

        return (SecurityEventType.Shutdown, "System Shutdown");
    }

    /// <summary>
    /// Determines whether a system event has already been recorded in history,
    /// avoiding duplicate alerts and duplicate records.
    /// Matches events within a 2-minute tolerance of the same EventType.
    /// </summary>
    public static bool IsDuplicateEvent(IEnumerable<Incident> existingIncidents, DateTime eventTime, SecurityEventType eventType)
    {
        return existingIncidents.Any(i =>
            i.EventType == eventType &&
            Math.Abs((i.DetectedAt - eventTime).TotalMinutes) < 2.0);
    }

    /// <summary>
    /// Formats Telegram notification message for a detected system event.
    /// </summary>
    public static string FormatSystemEventAlert(Incident incident)
    {
        string time = incident.DetectedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        return incident.EventType switch
        {
            SecurityEventType.Restart =>
                "🔄 LoginGuard Restart Detected\n\n" +
                $"Time: {time}\n" +
                (!string.IsNullOrWhiteSpace(incident.Details) ? $"{incident.Details}\n" : ""),

            SecurityEventType.UnexpectedShutdown =>
                "⚠️ Unexpected Shutdown Detected\n\n" +
                $"Time: {time}\n" +
                (!string.IsNullOrWhiteSpace(incident.Details) ? $"{incident.Details}\n" : ""),

            SecurityEventType.Shutdown =>
                "⚫ LoginGuard Shutdown Detected\n\n" +
                $"Time: {time}\n" +
                (!string.IsNullOrWhiteSpace(incident.Details) ? $"{incident.Details}\n" : ""),

            _ =>
                $"System Event: {incident.EventType} at {time}"
        };
    }

    /// <summary>
    /// Queries the Windows System event log for recent shutdown/restart events (Event ID 1074, 6008, 41)
    /// within the lookback window (default: past 24 hours).
    /// </summary>
    public static List<SystemShutdownEvent> QueryRecentShutdownEvents(TimeSpan? lookback = null)
    {
        var results = new List<SystemShutdownEvent>();
        var window = lookback ?? TimeSpan.FromHours(24);
        DateTime cutoff = DateTime.UtcNow - window;

        try
        {
            // Event ID 1074 (User32 shutdown/restart) and 6008 (unexpected shutdown)
            string queryStr = "*[System[(EventID=1074 or EventID=6008) and TimeCreated[timediff(@SystemTime) <= " + (long)window.TotalMilliseconds + "]]]";
            var query = new EventLogQuery("System", PathType.LogName, queryStr)
            {
                ReverseDirection = true
            };

            using var reader = new EventLogReader(query);
            EventRecord? record;

            while ((record = reader.ReadEvent()) != null)
            {
                using (record)
                {
                    try
                    {
                        DateTime recordTime = record.TimeCreated?.ToUniversalTime() ?? DateTime.UtcNow;
                        if (recordTime < cutoff) continue;

                        int eventId = record.Id;
                        string? shutdownType = null;
                        string? process = null;
                        string? user = null;
                        string? reason = null;

                        if (eventId == 1074 && record.Properties != null && record.Properties.Count >= 5)
                        {
                            process = record.Properties[0]?.Value?.ToString();
                            shutdownType = record.Properties[4]?.Value?.ToString();
                            user = record.Properties[6]?.Value?.ToString() ?? record.Properties.LastOrDefault()?.Value?.ToString();
                            reason = record.Properties[2]?.Value?.ToString();
                        }

                        var (eventType, details) = ClassifyEvent(eventId, shutdownType, process, user, reason);

                        results.Add(new SystemShutdownEvent(
                            RecordId: record.RecordId,
                            Timestamp: recordTime,
                            EventId: eventId,
                            EventType: eventType,
                            InitiatingUser: user,
                            Process: process,
                            Details: details
                        ));
                    }
                    catch
                    {
                        // Safely skip any record read error
                    }
                }
            }
        }
        catch
        {
            // Event log reader might fail if log is unavailable or permissions restricted; graceful fallback
        }

        return results;
    }
}

using System;

namespace LoginGuardService;

public enum CaptureStatus
{
    Pending,
    Success,
    Failed
}

public enum NotificationStatus
{
    NotAttempted,
    Sent,
    Queued,
    Retrying,
    PermanentFailure
}

public class Incident
{
    public Guid IncidentId { get; init; } = Guid.NewGuid();
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
    public string TargetUser { get; init; } = string.Empty;
    public string Workstation { get; init; } = string.Empty;
    public int LogonType { get; init; }
    public CaptureStatus CaptureStatus { get; set; } = CaptureStatus.Pending;
    public string? CapturePath { get; set; }
    public NotificationStatus NotificationStatus { get; set; } = NotificationStatus.NotAttempted;
    public int NotificationAttempts { get; set; } = 0;
    public DateTime? LastNotificationAttempt { get; set; }
    public string? LastNotificationError { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

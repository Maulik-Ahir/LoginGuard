using System;

namespace LoginGuardService;

public enum SecurityEventType
{
    FailedLogin = 0,
    USBStorageInserted = 1,
    Shutdown = 2,
    Restart = 3,
    UnexpectedShutdown = 4,
    RemoteLock = 5
}

public enum CaptureStatus
{
    Pending,
    Success,
    Failed,
    NotApplicable
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
    public SecurityEventType EventType { get; set; } = SecurityEventType.FailedLogin;
    public string TargetUser { get; init; } = string.Empty;
    public string Workstation { get; init; } = string.Empty;
    public int LogonType { get; init; }
    public CaptureStatus CaptureStatus { get; set; } = CaptureStatus.Pending;
    public string? CapturePath { get; set; }
    public NotificationStatus NotificationStatus { get; set; } = NotificationStatus.NotAttempted;
    public int NotificationAttempts { get; set; } = 0;
    public DateTime? LastNotificationAttempt { get; set; }
    public string? LastNotificationError { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceDescription { get; set; }
    public string? Manufacturer { get; set; }
    public string? DeviceClass { get; set; }
    public string? HardwareId { get; set; }
    public string? DeviceId { get; set; }
    public string? DriveLetter { get; set; }
    public string? VolumeLabel { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

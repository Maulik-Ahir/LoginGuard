using System;
using System.IO;
using LoginGuardService;
using Xunit;

namespace LoginGuard.Tests;

public class IncidentHistoryTests : IDisposable
{
    private readonly string _tempHistoryDir;

    public IncidentHistoryTests()
    {
        _tempHistoryDir = Path.Combine(Path.GetTempPath(), "LoginGuard_Test_History_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempHistoryDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempHistoryDir))
            {
                Directory.Delete(_tempHistoryDir, recursive: true);
            }
        }
        catch
        {
            // Best effort cleanup
        }
    }

    [Fact]
    public void Incident_DefaultValues_AreSensible()
    {
        var incident = new Incident();

        Assert.NotEqual(Guid.Empty, incident.IncidentId);
        Assert.Equal(CaptureStatus.Pending, incident.CaptureStatus);
        Assert.Equal(NotificationStatus.NotAttempted, incident.NotificationStatus);
        Assert.Equal(0, incident.NotificationAttempts);
        Assert.Null(incident.CapturePath);
        Assert.Null(incident.LastNotificationAttempt);
        Assert.Null(incident.LastNotificationError);
        Assert.True((DateTime.UtcNow - incident.DetectedAt).TotalSeconds < 10);
    }

    [Fact]
    public void SaveAndLoadIncident_RoundTrips_AllFieldsCorrectly()
    {
        var incidentId = Guid.NewGuid();
        var detectedAt = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc);
        var incident = new Incident
        {
            IncidentId = incidentId,
            DetectedAt = detectedAt,
            TargetUser = "testuser",
            Workstation = "WORKSTATION-01",
            LogonType = 2,
            CaptureStatus = CaptureStatus.Success,
            CapturePath = @"C:\Fake\capture.jpg",
            NotificationStatus = NotificationStatus.Sent,
            NotificationAttempts = 1,
            LastNotificationAttempt = detectedAt.AddSeconds(5),
            LastNotificationError = null
        };

        IncidentRepository.SaveIncident(_tempHistoryDir, incident);

        var loaded = IncidentRepository.GetIncidentById(_tempHistoryDir, incidentId);

        Assert.NotNull(loaded);
        Assert.Equal(incidentId, loaded!.IncidentId);
        Assert.Equal(detectedAt, loaded.DetectedAt);
        Assert.Equal("testuser", loaded.TargetUser);
        Assert.Equal("WORKSTATION-01", loaded.Workstation);
        Assert.Equal(2, loaded.LogonType);
        Assert.Equal(CaptureStatus.Success, loaded.CaptureStatus);
        Assert.Equal(@"C:\Fake\capture.jpg", loaded.CapturePath);
        Assert.Equal(NotificationStatus.Sent, loaded.NotificationStatus);
        Assert.Equal(1, loaded.NotificationAttempts);
        Assert.Equal(detectedAt.AddSeconds(5), loaded.LastNotificationAttempt);
        Assert.Null(loaded.LastNotificationError);
    }

    [Fact]
    public void UpdateIncident_UpdatesExistingFileWithoutCreatingDuplicate()
    {
        var incident = new Incident
        {
            TargetUser = "admin",
            Workstation = "DESKTOP-TEST",
            LogonType = 7,
            CaptureStatus = CaptureStatus.Pending,
            NotificationStatus = NotificationStatus.NotAttempted
        };

        IncidentRepository.SaveIncident(_tempHistoryDir, incident);

        var filesBefore = Directory.GetFiles(_tempHistoryDir, "*.json");
        Assert.Single(filesBefore);

        // Update lifecycle state
        incident.CaptureStatus = CaptureStatus.Success;
        incident.CapturePath = @"C:\Captures\test.jpg";
        incident.NotificationStatus = NotificationStatus.Queued;
        incident.NotificationAttempts = 1;
        incident.LastNotificationAttempt = DateTime.UtcNow;

        IncidentRepository.SaveIncident(_tempHistoryDir, incident);

        var filesAfter = Directory.GetFiles(_tempHistoryDir, "*.json");
        Assert.Single(filesAfter);

        var updated = IncidentRepository.GetIncidentById(_tempHistoryDir, incident.IncidentId);
        Assert.NotNull(updated);
        Assert.Equal(CaptureStatus.Success, updated!.CaptureStatus);
        Assert.Equal(@"C:\Captures\test.jpg", updated.CapturePath);
        Assert.Equal(NotificationStatus.Queued, updated.NotificationStatus);
        Assert.Equal(1, updated.NotificationAttempts);
    }

    [Fact]
    public void LoadAllIncidents_ReturnsDescendingByDetectedAt()
    {
        var inc1 = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-30),
            TargetUser = "user1"
        };
        var inc2 = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-10),
            TargetUser = "user2"
        };
        var inc3 = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-20),
            TargetUser = "user3"
        };

        IncidentRepository.SaveIncident(_tempHistoryDir, inc1);
        IncidentRepository.SaveIncident(_tempHistoryDir, inc2);
        IncidentRepository.SaveIncident(_tempHistoryDir, inc3);

        var list = IncidentRepository.LoadAllIncidents(_tempHistoryDir);

        Assert.Equal(3, list.Count);
        Assert.Equal("user2", list[0].TargetUser); // newest
        Assert.Equal("user3", list[1].TargetUser);
        Assert.Equal("user1", list[2].TargetUser); // oldest
    }

    [Fact]
    public void LoadIncident_CorruptOrInvalidFile_ReturnsNullGracefully()
    {
        string corruptFilePath = Path.Combine(_tempHistoryDir, "incident_corrupt.json");
        File.WriteAllText(corruptFilePath, "{ invalid json content [[[");

        var result = IncidentRepository.LoadIncident(corruptFilePath);
        Assert.Null(result);

        // LoadAllIncidents should also safely skip corrupt files
        var validIncident = new Incident { TargetUser = "validUser" };
        IncidentRepository.SaveIncident(_tempHistoryDir, validIncident);

        var all = IncidentRepository.LoadAllIncidents(_tempHistoryDir);
        Assert.Single(all);
        Assert.Equal("validUser", all[0].TargetUser);
    }

    [Fact]
    public void LoadIncident_NonExistentFile_ReturnsNull()
    {
        var result = IncidentRepository.LoadIncident(Path.Combine(_tempHistoryDir, "does_not_exist.json"));
        Assert.Null(result);

        var byId = IncidentRepository.GetIncidentById(_tempHistoryDir, Guid.NewGuid());
        Assert.Null(byId);
    }

    [Fact]
    public void CleanupOldHistory_DeletesFilesOlderThanRetentionPeriod()
    {
        var oldIncident = new Incident { TargetUser = "oldUser" };
        var newIncident = new Incident { TargetUser = "newUser" };

        IncidentRepository.SaveIncident(_tempHistoryDir, oldIncident);
        IncidentRepository.SaveIncident(_tempHistoryDir, newIncident);

        string? oldFile = IncidentRepository.FindIncidentFile(_tempHistoryDir, oldIncident.IncidentId);
        Assert.NotNull(oldFile);

        // Set last write time of old file to 40 days ago
        File.SetLastWriteTimeUtc(oldFile!, DateTime.UtcNow.AddDays(-40));

        int deleted = IncidentRepository.CleanupOldHistory(_tempHistoryDir, retentionDays: 30);

        Assert.Equal(1, deleted);
        Assert.Null(IncidentRepository.GetIncidentById(_tempHistoryDir, oldIncident.IncidentId));
        Assert.NotNull(IncidentRepository.GetIncidentById(_tempHistoryDir, newIncident.IncidentId));
    }

    [Fact]
    public void CleanupOldHistory_RetentionNeverNegative1_DeletesNothing()
    {
        var oldIncident = new Incident { TargetUser = "oldUser" };
        IncidentRepository.SaveIncident(_tempHistoryDir, oldIncident);

        string? oldFile = IncidentRepository.FindIncidentFile(_tempHistoryDir, oldIncident.IncidentId);
        Assert.NotNull(oldFile);
        File.SetLastWriteTimeUtc(oldFile!, DateTime.UtcNow.AddDays(-400));

        int deleted = IncidentRepository.CleanupOldHistory(_tempHistoryDir, retentionDays: -1);

        Assert.Equal(0, deleted);
        Assert.NotNull(IncidentRepository.GetIncidentById(_tempHistoryDir, oldIncident.IncidentId));
    }
}

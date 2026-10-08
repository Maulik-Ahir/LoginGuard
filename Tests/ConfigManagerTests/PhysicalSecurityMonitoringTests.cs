using System;
using System.Collections.Generic;
using System.IO;
using LoginGuardService;
using LoginGuardUI;
using Xunit;

namespace LoginGuard.Tests;

public class PhysicalSecurityMonitoringTests : IDisposable
{
    private readonly string _tempHistoryDir;

    public PhysicalSecurityMonitoringTests()
    {
        _tempHistoryDir = Path.Combine(Path.GetTempPath(), "LoginGuard_Test_Physical_" + Guid.NewGuid().ToString("N"));
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
        catch { }
    }

    // ══════════════════════════════════════════════════════
    //  USB Storage Detection Tests
    // ══════════════════════════════════════════════════════

    [Fact]
    public void UsbStorageDetector_NewInsertion_CreatesIncidentWithCorrectProperties()
    {
        var detector = new UsbStorageDetector();

        var drives = new List<UsbDriveSnapshot>
        {
            new UsbDriveSnapshot(
                RootDirectory: @"E:\",
                DriveLetter: "E:",
                VolumeLabel: "KINGSTON",
                DriveFormat: "FAT32",
                TotalSizeInBytes: 16000000000L
            )
        };

        var incidents = detector.EvaluateDriveChanges(drives, "MY-LAPTOP");

        Assert.Single(incidents);
        var inc = incidents[0];
        Assert.Equal(SecurityEventType.USBStorageInserted, inc.EventType);
        Assert.Equal("E:", inc.DriveLetter);
        Assert.Equal("KINGSTON", inc.VolumeLabel);
        Assert.Equal("KINGSTON (E:)", inc.DeviceName);
        Assert.Equal(CaptureStatus.NotApplicable, inc.CaptureStatus);
        Assert.Null(inc.CapturePath);
        Assert.Equal(NotificationStatus.NotAttempted, inc.NotificationStatus);
        Assert.Equal("MY-LAPTOP", inc.Workstation);
    }

    [Fact]
    public void UsbStorageDetector_DuplicateCheck_DoesNotRecreateIncident()
    {
        var detector = new UsbStorageDetector();

        var drives = new List<UsbDriveSnapshot>
        {
            new UsbDriveSnapshot(@"E:\", "E:", "SANDISK", "NTFS", 32000000000L)
        };

        var firstPass = detector.EvaluateDriveChanges(drives);
        Assert.Single(firstPass);

        // Second pass with same drive connected -> zero new incidents
        var secondPass = detector.EvaluateDriveChanges(drives);
        Assert.Empty(secondPass);
    }

    [Fact]
    public void UsbStorageDetector_RemovalAndReinsertion_TriggersAgain()
    {
        var detector = new UsbStorageDetector();

        var drive = new UsbDriveSnapshot(@"F:\", "F:", "BACKUP", "exFAT", 64000000000L);

        // 1. Inserted
        var pass1 = detector.EvaluateDriveChanges(new[] { drive });
        Assert.Single(pass1);

        // 2. Removed (empty active drives)
        var pass2 = detector.EvaluateDriveChanges(Array.Empty<UsbDriveSnapshot>());
        Assert.Empty(pass2);

        // 3. Re-inserted
        var pass3 = detector.EvaluateDriveChanges(new[] { drive });
        Assert.Single(pass3);
        Assert.Equal("F:", pass3[0].DriveLetter);
    }

    [Theory]
    [InlineData(@"E:\", "E:")]
    [InlineData("e:", "E:")]
    [InlineData("F", "F")]
    [InlineData(@"g:\some\subpath", "G:")]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    public void UsbStorageDetector_NormalizeDriveLetter_BehavesPredictably(string? input, string expected)
    {
        Assert.Equal(expected, UsbStorageDetector.NormalizeDriveLetter(input!));
    }

    // ══════════════════════════════════════════════════════
    //  Telegram /usb Command & Alerts
    // ══════════════════════════════════════════════════════

    [Theory]
    [InlineData("/usb", "/usb")]
    [InlineData("/USB", "/usb")]
    [InlineData("  /Usb  ", "/usb")]
    [InlineData("/usb@LoginGuardBot", "/usb")]
    public void Telegram_ParseUsbCommand(string input, string expected)
    {
        Assert.Equal(expected, TelegramCommandHandler.ParseCommand(input));
        Assert.True(TelegramCommandHandler.IsKnownCommand(expected));
    }

    [Fact]
    public void Telegram_FormatUsbAlert_ContainsExpectedFields()
    {
        var incident = new Incident
        {
            DetectedAt = new DateTime(2026, 10, 8, 21, 30, 12, DateTimeKind.Utc),
            EventType = SecurityEventType.USBStorageInserted,
            DeviceName = "Kingston DataTraveler",
            DriveLetter = "E:",
            VolumeLabel = "Kingston DataTraveler"
        };

        string alert = TelegramCommandHandler.FormatUsbAlert(incident);

        Assert.Contains("USB Storage Detected", alert);
        Assert.Contains("Kingston DataTraveler", alert);
        Assert.Contains("E:", alert);
    }

    [Fact]
    public void Telegram_FormatRecentUsbActivity_Empty_ReturnsNoActivity()
    {
        string result = TelegramCommandHandler.FormatRecentUsbActivity(_tempHistoryDir);
        Assert.Equal("No USB activity recorded.", result);
    }

    [Fact]
    public void Telegram_FormatRecentUsbActivity_WithUsbEvents_FormatsCorrectly()
    {
        var usb1 = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-10),
            EventType = SecurityEventType.USBStorageInserted,
            VolumeLabel = "Kingston",
            DriveLetter = "E:"
        };
        var usb2 = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-5),
            EventType = SecurityEventType.USBStorageInserted,
            VolumeLabel = "SanDisk",
            DriveLetter = "F:"
        };
        var login = new Incident
        {
            DetectedAt = DateTime.UtcNow.AddMinutes(-2),
            EventType = SecurityEventType.FailedLogin,
            TargetUser = "admin"
        };

        IncidentRepository.SaveIncident(_tempHistoryDir, usb1);
        IncidentRepository.SaveIncident(_tempHistoryDir, usb2);
        IncidentRepository.SaveIncident(_tempHistoryDir, login);

        string result = TelegramCommandHandler.FormatRecentUsbActivity(_tempHistoryDir);

        Assert.Contains("Recent USB Activity", result);
        Assert.Contains("SanDisk", result);
        Assert.Contains("Kingston", result);
        Assert.DoesNotContain("admin", result); // Non-USB event excluded
    }

    // ══════════════════════════════════════════════════════
    //  Shutdown / Restart Detection Tests
    // ══════════════════════════════════════════════════════

    [Fact]
    public void SystemEventDetector_Classify1074_Restart()
    {
        var (type, details) = SystemEventDetector.ClassifyEvent(
            eventId: 1074,
            shutdownType: "restart",
            process: @"C:\Windows\System32\shutdown.exe",
            user: "MAULIK-PC\\admin",
            reason: "Application: Maintenance"
        );

        Assert.Equal(SecurityEventType.Restart, type);
        Assert.Contains("Restart", details);
        Assert.Contains("admin", details);
        Assert.Contains("shutdown.exe", details);
    }

    [Fact]
    public void SystemEventDetector_Classify1074_NormalShutdown()
    {
        var (type, details) = SystemEventDetector.ClassifyEvent(
            eventId: 1074,
            shutdownType: "power off",
            process: @"C:\Windows\System32\RuntimeBroker.exe",
            user: "NT AUTHORITY\\SYSTEM",
            reason: "Operating System: Upgrade (Planned)"
        );

        Assert.Equal(SecurityEventType.Shutdown, type);
        Assert.Contains("Normal Shutdown", details);
        Assert.Contains("SYSTEM", details);
    }

    [Theory]
    [InlineData(6008)]
    [InlineData(41)]
    public void SystemEventDetector_ClassifyUnexpectedShutdown(int eventId)
    {
        var (type, details) = SystemEventDetector.ClassifyEvent(
            eventId: eventId,
            shutdownType: null,
            process: null,
            user: null,
            reason: null
        );

        Assert.Equal(SecurityEventType.UnexpectedShutdown, type);
        Assert.Contains("unexpected", details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemEventDetector_IsDuplicateEvent_DetectsDuplicates()
    {
        var baseTime = DateTime.UtcNow;
        var existing = new List<Incident>
        {
            new Incident
            {
                DetectedAt = baseTime,
                EventType = SecurityEventType.Restart
            }
        };

        // Event within 2 minutes of same type is duplicate
        Assert.True(SystemEventDetector.IsDuplicateEvent(existing, baseTime.AddSeconds(30), SecurityEventType.Restart));

        // Different type is NOT duplicate
        Assert.False(SystemEventDetector.IsDuplicateEvent(existing, baseTime.AddSeconds(30), SecurityEventType.Shutdown));

        // Event hours away is NOT duplicate
        Assert.False(SystemEventDetector.IsDuplicateEvent(existing, baseTime.AddHours(3), SecurityEventType.Restart));
    }

    [Fact]
    public void SystemEventDetector_FormatAlert_ContainsExpectedText()
    {
        var restart = new Incident
        {
            DetectedAt = DateTime.UtcNow,
            EventType = SecurityEventType.Restart,
            Details = "Type: Restart | Initiated by: admin"
        };
        string restartMsg = SystemEventDetector.FormatSystemEventAlert(restart);
        Assert.Contains("Restart Detected", restartMsg);
        Assert.Contains("admin", restartMsg);

        var crash = new Incident
        {
            DetectedAt = DateTime.UtcNow,
            EventType = SecurityEventType.UnexpectedShutdown,
            Details = "Power loss"
        };
        string crashMsg = SystemEventDetector.FormatSystemEventAlert(crash);
        Assert.Contains("Unexpected Shutdown Detected", crashMsg);
    }

    // ══════════════════════════════════════════════════════
    //  UI Helper & Filter Tests
    // ══════════════════════════════════════════════════════

    [Fact]
    public void IncidentHistoryHelper_NewFilters_FilterCorrectly()
    {
        var list = new List<Incident>
        {
            new Incident { EventType = SecurityEventType.FailedLogin },
            new Incident { EventType = SecurityEventType.USBStorageInserted },
            new Incident { EventType = SecurityEventType.Shutdown },
            new Incident { EventType = SecurityEventType.Restart },
            new Incident { EventType = SecurityEventType.UnexpectedShutdown }
        };

        var failedOnly = IncidentHistoryHelper.ApplyFilter(list, IncidentHistoryHelper.IncidentFilter.FailedLoginsOnly);
        Assert.Single(failedOnly);
        Assert.Equal(SecurityEventType.FailedLogin, failedOnly[0].EventType);

        var usbOnly = IncidentHistoryHelper.ApplyFilter(list, IncidentHistoryHelper.IncidentFilter.UsbOnly);
        Assert.Single(usbOnly);
        Assert.Equal(SecurityEventType.USBStorageInserted, usbOnly[0].EventType);

        var sysOnly = IncidentHistoryHelper.ApplyFilter(list, IncidentHistoryHelper.IncidentFilter.ShutdownRestartOnly);
        Assert.Equal(3, sysOnly.Count);
    }

    [Fact]
    public void IncidentHistoryHelper_Summary_IncludesUsbCount()
    {
        var list = new List<Incident>
        {
            new Incident { EventType = SecurityEventType.FailedLogin, CaptureStatus = CaptureStatus.Success },
            new Incident { EventType = SecurityEventType.USBStorageInserted, DeviceName = "SanDisk" },
            new Incident { EventType = SecurityEventType.USBStorageInserted, DeviceName = "Kingston" }
        };

        var summary = IncidentHistoryHelper.CalculateSummary(list);
        Assert.Equal(3, summary.TotalIncidents);
        Assert.Equal(1, summary.SuccessfulCaptures);
        Assert.Equal(2, summary.UsbEvents);
    }

    // ══════════════════════════════════════════════════════
    //  Backward Compatibility Tests
    // ══════════════════════════════════════════════════════

    [Fact]
    public void IncidentRepository_LoadsLegacyV08Json_DefaultsToFailedLogin()
    {
        // Old v0.8 JSON without "EventType" property
        string legacyJson = """
        {
          "IncidentId": "11111111-2222-3333-4444-555555555555",
          "DetectedAt": "2026-10-02T12:00:00Z",
          "TargetUser": "legacyUser",
          "Workstation": "LAPTOP",
          "LogonType": 7,
          "CaptureStatus": "Success",
          "NotificationStatus": "Sent"
        }
        """;

        string filePath = Path.Combine(_tempHistoryDir, "incident_legacy.json");
        File.WriteAllText(filePath, legacyJson);

        var loaded = IncidentRepository.LoadIncident(filePath);

        Assert.NotNull(loaded);
        Assert.Equal(SecurityEventType.FailedLogin, loaded!.EventType);
        Assert.Equal("legacyUser", loaded.TargetUser);
        Assert.Equal(CaptureStatus.Success, loaded.CaptureStatus);
    }
}

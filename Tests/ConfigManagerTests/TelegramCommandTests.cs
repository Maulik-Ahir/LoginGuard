using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using LoginGuardService;
using LoginGuardUI;
using Xunit;

namespace LoginGuard.Tests;

public class TelegramCommandTests : IDisposable
{
    private readonly string _tempHistoryDir;
    private readonly string _tempPendingDir;

    public TelegramCommandTests()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "LoginGuard_Test_Cmd_" + Guid.NewGuid().ToString("N"));
        _tempHistoryDir = Path.Combine(baseDir, "History");
        _tempPendingDir = Path.Combine(baseDir, "Pending");
        Directory.CreateDirectory(_tempHistoryDir);
        Directory.CreateDirectory(_tempPendingDir);
    }

    public void Dispose()
    {
        try
        {
            string baseDir = Path.GetDirectoryName(_tempHistoryDir)!;
            if (Directory.Exists(baseDir))
                Directory.Delete(baseDir, recursive: true);
        }
        catch { }
    }

    // ══════════════════════════════════════════════════════
    //  Command Parsing
    // ══════════════════════════════════════════════════════

    [Theory]
    [InlineData("/lock", "/lock")]
    [InlineData("/LOCK", "/lock")]
    [InlineData("/Lock", "/lock")]
    [InlineData("  /status  ", "/status")]
    [InlineData("/help", "/help")]
    [InlineData("/HISTORY", "/history")]
    [InlineData("/Pending", "/pending")]
    [InlineData("/last", "/last")]
    public void ParseCommand_CaseInsensitive_AndTrimsWhitespace(string input, string expected)
    {
        Assert.Equal(expected, TelegramCommandHandler.ParseCommand(input));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ParseCommand_EmptyOrNull_ReturnsEmpty(string? input, string expected)
    {
        Assert.Equal(expected, TelegramCommandHandler.ParseCommand(input));
    }

    [Fact]
    public void ParseCommand_BotMention_StripsAtSuffix()
    {
        Assert.Equal("/lock", TelegramCommandHandler.ParseCommand("/lock@MyBot"));
        Assert.Equal("/status", TelegramCommandHandler.ParseCommand("/STATUS@LoginGuardBot"));
    }

    [Theory]
    [InlineData("/help", true)]
    [InlineData("/status", true)]
    [InlineData("/last", true)]
    [InlineData("/history", true)]
    [InlineData("/pending", true)]
    [InlineData("/lock", true)]
    [InlineData("/unlock", false)]
    [InlineData("/delete", false)]
    [InlineData("/unknown", false)]
    [InlineData("hello", false)]
    public void IsKnownCommand_ClassifiesCorrectly(string command, bool expected)
    {
        Assert.Equal(expected, TelegramCommandHandler.IsKnownCommand(command));
    }

    // ══════════════════════════════════════════════════════
    //  Unknown Command
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatUnknownCommand_ReturnsExpectedMessage()
    {
        string result = TelegramCommandHandler.FormatUnknownCommand();
        Assert.Contains("/help", result);
        Assert.Contains("Unknown command", result);
    }

    // ══════════════════════════════════════════════════════
    //  /help
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatHelp_ContainsAllCommands()
    {
        string help = TelegramCommandHandler.FormatHelp();
        Assert.Contains("/status", help);
        Assert.Contains("/last", help);
        Assert.Contains("/history", help);
        Assert.Contains("/pending", help);
        Assert.Contains("/lock", help);
        Assert.Contains("/help", help);
    }

    // ══════════════════════════════════════════════════════
    //  /status
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatStatus_ContainsServiceInfo()
    {
        string status = TelegramCommandHandler.FormatStatus(0, 2, "14:30:00", "Sent");
        Assert.Contains("Service: Running", status);
        Assert.Contains("Camera: Configured (Index 0)", status);
        Assert.Contains("Pending Alerts: 2", status);
        Assert.Contains("Last Incident: 14:30:00", status);
        Assert.Contains("Last Notification: Sent", status);
    }

    [Fact]
    public void FormatStatus_NoIncidents_ShowsNone()
    {
        string status = TelegramCommandHandler.FormatStatus(1, 0, null, null);
        Assert.Contains("Last Incident: None", status);
        Assert.Contains("Last Notification: None", status);
        Assert.Contains("Pending Alerts: 0", status);
    }

    // ══════════════════════════════════════════════════════
    //  /last
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatLastIncident_NoIncidents_ReturnsNoIncidentsMessage()
    {
        var result = TelegramCommandHandler.FormatLastIncident(_tempHistoryDir);
        Assert.Equal("No incidents recorded yet.", result.Message);
        Assert.Null(result.CaptureFilePath);
    }

    [Fact]
    public void FormatLastIncident_WithIncident_ShowsDetails()
    {
        var incident = new Incident
        {
            DetectedAt = new DateTime(2026, 10, 2, 14, 42, 0, DateTimeKind.Utc),
            TargetUser = "Maulik",
            Workstation = "LAPTOP",
            LogonType = 7,
            CaptureStatus = CaptureStatus.Success,
            CapturePath = @"C:\NonExistent\fake.jpg", // file doesn't exist
            NotificationStatus = NotificationStatus.Sent
        };
        IncidentRepository.SaveIncident(_tempHistoryDir, incident);

        var result = TelegramCommandHandler.FormatLastIncident(_tempHistoryDir);
        Assert.Contains("Maulik", result.Message);
        Assert.Contains("Unlock (7)", result.Message);
        Assert.Contains("LAPTOP", result.Message);
        Assert.Contains("✅ Success", result.Message);
        Assert.Contains("✅ Sent", result.Message);
        // File doesn't exist, so should indicate unavailable
        Assert.Contains("unavailable", result.Message);
        Assert.Null(result.CaptureFilePath);
    }

    [Fact]
    public void FormatLastIncident_WithValidCapture_ReturnsCaptureFilePath()
    {
        // Create a real temp file to simulate capture
        string tempCapture = Path.Combine(_tempHistoryDir, "test_capture.jpg");
        File.WriteAllText(tempCapture, "fake image data");

        var incident = new Incident
        {
            DetectedAt = DateTime.UtcNow,
            TargetUser = "TestUser",
            Workstation = "PC",
            LogonType = 2,
            CaptureStatus = CaptureStatus.Success,
            CapturePath = tempCapture,
            NotificationStatus = NotificationStatus.Sent
        };
        IncidentRepository.SaveIncident(_tempHistoryDir, incident);

        var result = TelegramCommandHandler.FormatLastIncident(_tempHistoryDir);
        Assert.Contains("TestUser", result.Message);
        Assert.Equal(tempCapture, result.CaptureFilePath);
    }

    [Fact]
    public void FormatLastIncident_MissingHistoryDir_ReturnsNoIncidents()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), "LG_NonExistent_" + Guid.NewGuid().ToString("N"));
        var result = TelegramCommandHandler.FormatLastIncident(nonExistent);
        Assert.Equal("No incidents recorded yet.", result.Message);
    }

    // ══════════════════════════════════════════════════════
    //  /history
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatHistory_NoIncidents_ReturnsMessage()
    {
        string result = TelegramCommandHandler.FormatHistory(_tempHistoryDir);
        Assert.Equal("No incidents recorded yet.", result);
    }

    [Fact]
    public void FormatHistory_MultipleIncidents_ShowsUpTo5()
    {
        for (int i = 0; i < 7; i++)
        {
            var inc = new Incident
            {
                DetectedAt = DateTime.UtcNow.AddMinutes(-i * 10),
                TargetUser = $"User{i}",
                LogonType = 2,
                CaptureStatus = i % 2 == 0 ? CaptureStatus.Success : CaptureStatus.Failed,
                NotificationStatus = i % 2 == 0 ? NotificationStatus.Sent : NotificationStatus.PermanentFailure
            };
            IncidentRepository.SaveIncident(_tempHistoryDir, inc);
        }

        string result = TelegramCommandHandler.FormatHistory(_tempHistoryDir);
        Assert.Contains("Recent Incidents", result);
        Assert.Contains("User0", result);
        Assert.Contains("User4", result);
        Assert.DoesNotContain("User5", result); // Only top 5
        Assert.Contains("2 more incidents not shown", result);
    }

    [Fact]
    public void FormatHistory_FewIncidents_ShowsAll()
    {
        var inc = new Incident
        {
            DetectedAt = DateTime.UtcNow,
            TargetUser = "Admin",
            LogonType = 10,
            CaptureStatus = CaptureStatus.Success,
            NotificationStatus = NotificationStatus.Sent
        };
        IncidentRepository.SaveIncident(_tempHistoryDir, inc);

        string result = TelegramCommandHandler.FormatHistory(_tempHistoryDir);
        Assert.Contains("Admin", result);
        Assert.Contains("RemoteInteractive (10)", result);
        Assert.DoesNotContain("more incidents not shown", result);
    }

    // ══════════════════════════════════════════════════════
    //  /pending
    // ══════════════════════════════════════════════════════

    [Fact]
    public void FormatPending_EmptyQueue_ReturnsNoPending()
    {
        string result = TelegramCommandHandler.FormatPending(_tempPendingDir);
        Assert.Equal("No pending notifications.", result);
    }

    [Fact]
    public void FormatPending_WithQueuedItems_ShowsCount()
    {
        // Create fake pending notification files
        for (int i = 0; i < 3; i++)
        {
            var record = new
            {
                PhotoPath = $@"C:\Fake\photo{i}.jpg",
                TargetUser = $"user{i}",
                QueuedAt = DateTime.UtcNow.AddMinutes(-10 + i)
            };
            string path = Path.Combine(_tempPendingDir, $"pending_{i}.json");
            File.WriteAllText(path, JsonSerializer.Serialize(record));
        }

        string result = TelegramCommandHandler.FormatPending(_tempPendingDir);
        Assert.Contains("Queued: 3", result);
        Assert.Contains("Oldest:", result);
        Assert.Contains("Newest:", result);
    }

    [Fact]
    public void FormatPending_NonExistentDir_ReturnsNoPending()
    {
        string nonExistent = Path.Combine(Path.GetTempPath(), "LG_NoPending_" + Guid.NewGuid().ToString("N"));
        string result = TelegramCommandHandler.FormatPending(nonExistent);
        Assert.Equal("No pending notifications.", result);
    }
}

// ══════════════════════════════════════════════════════
//  Batch 2 — IncidentHistoryHelper Tests
// ══════════════════════════════════════════════════════

public class IncidentHistoryHelperTests
{
    [Theory]
    [InlineData(2, "Interactive (2)")]
    [InlineData(7, "Unlock (7)")]
    [InlineData(10, "RemoteInteractive (10)")]
    [InlineData(99, "Type 99")]
    public void FormatLogonType_ReturnsExpectedNames(int logonType, string expected)
    {
        Assert.Equal(expected, IncidentHistoryHelper.FormatLogonType(logonType));
    }

    [Fact]
    public void FormatCaptureStatus_AllValues()
    {
        Assert.Contains("Success", IncidentHistoryHelper.FormatCaptureStatus(CaptureStatus.Success));
        Assert.Contains("Failed", IncidentHistoryHelper.FormatCaptureStatus(CaptureStatus.Failed));
        Assert.Contains("Pending", IncidentHistoryHelper.FormatCaptureStatus(CaptureStatus.Pending));
    }

    [Fact]
    public void FormatNotificationStatus_AllValues()
    {
        Assert.Contains("Sent", IncidentHistoryHelper.FormatNotificationStatus(NotificationStatus.Sent));
        Assert.Contains("Queued", IncidentHistoryHelper.FormatNotificationStatus(NotificationStatus.Queued));
        Assert.Contains("Retrying", IncidentHistoryHelper.FormatNotificationStatus(NotificationStatus.Retrying));
        Assert.Contains("Failed", IncidentHistoryHelper.FormatNotificationStatus(NotificationStatus.PermanentFailure));
        Assert.Contains("Not Attempted", IncidentHistoryHelper.FormatNotificationStatus(NotificationStatus.NotAttempted));
    }

    [Fact]
    public void ApplyFilter_All_ReturnsAllIncidents()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.All);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void ApplyFilter_CaptureSuccessful_FiltersCorrectly()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.CaptureSuccessful);
        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(CaptureStatus.Success, i.CaptureStatus));
    }

    [Fact]
    public void ApplyFilter_CaptureFailed_FiltersCorrectly()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.CaptureFailed);
        Assert.Single(result);
        Assert.Equal(CaptureStatus.Failed, result[0].CaptureStatus);
    }

    [Fact]
    public void ApplyFilter_TelegramSent_FiltersCorrectly()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.TelegramSent);
        Assert.Single(result);
    }

    [Fact]
    public void ApplyFilter_TelegramPending_IncludesQueuedAndRetrying()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.TelegramPending);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void ApplyFilter_TelegramFailed_FiltersCorrectly()
    {
        var incidents = CreateTestIncidents();
        var result = IncidentHistoryHelper.ApplyFilter(incidents, IncidentHistoryHelper.IncidentFilter.TelegramFailed);
        Assert.Single(result);
    }

    [Fact]
    public void CalculateSummary_EmptyList_ReturnsDefaults()
    {
        var summary = IncidentHistoryHelper.CalculateSummary(new List<Incident>());
        Assert.Equal(0, summary.TotalIncidents);
        Assert.Equal(0, summary.SuccessfulCaptures);
        Assert.Equal(0, summary.TelegramSent);
        Assert.Equal(0, summary.TelegramPending);
        Assert.Equal("No incidents recorded", summary.LatestIncidentInfo);
    }

    [Fact]
    public void CalculateSummary_WithIncidents_ComputesCorrectly()
    {
        var now = DateTime.UtcNow;
        var incidents = new List<Incident>
        {
            new() { DetectedAt = now, TargetUser = "Admin", LogonType = 2, CaptureStatus = CaptureStatus.Success, NotificationStatus = NotificationStatus.Sent },
            new() { DetectedAt = now.AddMinutes(-5), TargetUser = "User1", LogonType = 7, CaptureStatus = CaptureStatus.Failed, NotificationStatus = NotificationStatus.Queued },
            new() { DetectedAt = now.AddMinutes(-10), TargetUser = "User2", LogonType = 10, CaptureStatus = CaptureStatus.Success, NotificationStatus = NotificationStatus.Retrying }
        };

        var summary = IncidentHistoryHelper.CalculateSummary(incidents);
        Assert.Equal(3, summary.TotalIncidents);
        Assert.Equal(2, summary.SuccessfulCaptures);
        Assert.Equal(1, summary.TelegramSent);
        Assert.Equal(2, summary.TelegramPending);
        Assert.Contains("Admin", summary.LatestIncidentInfo);
        Assert.Contains("Interactive (2)", summary.LatestIncidentInfo);
    }

    private static List<Incident> CreateTestIncidents()
    {
        return new List<Incident>
        {
            new() { CaptureStatus = CaptureStatus.Success, NotificationStatus = NotificationStatus.Sent },
            new() { CaptureStatus = CaptureStatus.Failed, NotificationStatus = NotificationStatus.PermanentFailure },
            new() { CaptureStatus = CaptureStatus.Success, NotificationStatus = NotificationStatus.Queued },
            new() { CaptureStatus = CaptureStatus.Pending, NotificationStatus = NotificationStatus.Retrying }
        };
    }
}

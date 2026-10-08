using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LoginGuardService;

public static class IncidentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetIncidentFilePath(string historyDir, Incident incident)
    {
        string timestamp = incident.DetectedAt.ToString("yyyyMMdd_HHmmss");
        string idShort = incident.IncidentId.ToString("N");
        return Path.Combine(historyDir, $"incident_{timestamp}_{idShort}.json");
    }

    public static void SaveIncident(string historyDir, Incident incident)
    {
        try
        {
            Directory.CreateDirectory(historyDir);
            incident.UpdatedAt = DateTime.UtcNow;

            string? existingPath = FindIncidentFile(historyDir, incident.IncidentId);
            string filePath = existingPath ?? GetIncidentFilePath(historyDir, incident);

            string tempPath = filePath + ".tmp";
            string json = JsonSerializer.Serialize(incident, JsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            // Suppress or handle appropriately
        }
    }

    public static Incident? LoadIncident(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Incident>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static Incident? GetIncidentById(string historyDir, Guid incidentId)
    {
        string? file = FindIncidentFile(historyDir, incidentId);
        if (file == null) return null;
        return LoadIncident(file);
    }

    public static string? FindIncidentFile(string historyDir, Guid incidentId)
    {
        try
        {
            if (!Directory.Exists(historyDir)) return null;
            string idShort = incidentId.ToString("N");
            var files = Directory.GetFiles(historyDir, $"*_{idShort}.json");
            return files.Length > 0 ? files[0] : null;
        }
        catch
        {
            return null;
        }
    }

    public static List<Incident> LoadAllIncidents(string historyDir)
    {
        var incidents = new List<Incident>();
        try
        {
            if (!Directory.Exists(historyDir)) return incidents;

            var files = Directory.GetFiles(historyDir, "incident_*.json");
            foreach (var file in files)
            {
                var inc = LoadIncident(file);
                if (inc != null)
                {
                    incidents.Add(inc);
                }
            }
        }
        catch
        {
            // Graceful degradation
        }

        incidents.Sort((a, b) => b.DetectedAt.CompareTo(a.DetectedAt));
        return incidents;
    }

    public static int CleanupOldHistory(string historyDir, int retentionDays)
    {
        if (retentionDays < 0) return 0; // -1 represents Never

        int deletedCount = 0;
        try
        {
            if (!Directory.Exists(historyDir)) return 0;

            var files = Directory.GetFiles(historyDir, "incident_*.json");
            DateTime cutoff = DateTime.UtcNow - TimeSpan.FromDays(retentionDays);

            foreach (var file in files)
            {
                try
                {
                    DateTime lastWriteUtc = File.GetLastWriteTimeUtc(file);
                    if (lastWriteUtc < cutoff)
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }
        catch
        {
            // Suppress top-level errors
        }

        return deletedCount;
    }
}

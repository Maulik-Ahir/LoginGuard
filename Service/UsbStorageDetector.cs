using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LoginGuardService;

public record UsbDriveSnapshot(
    string RootDirectory,
    string DriveLetter,
    string? VolumeLabel,
    string? DriveFormat,
    long? TotalSizeInBytes
);

/// <summary>
/// Detects USB and removable storage insertion in a Windows Service environment.
/// Keeps track of known mounted removable volumes to detect transitions reliably.
/// </summary>
public class UsbStorageDetector
{
    private readonly HashSet<string> _knownDriveLetters = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UsbStorageDetector(IEnumerable<string>? initialDrives = null)
    {
        if (initialDrives != null)
        {
            foreach (var d in initialDrives)
            {
                _knownDriveLetters.Add(NormalizeDriveLetter(d));
            }
        }
    }

    /// <summary>
    /// Initializes current drives at service start so already-connected drives don't trigger false alerts.
    /// </summary>
    public void InitializeFromCurrentDrives()
    {
        lock (_lock)
        {
            try
            {
                var currentRemovables = GetCurrentRemovableDrives();
                _knownDriveLetters.Clear();
                foreach (var drive in currentRemovables)
                {
                    _knownDriveLetters.Add(drive.DriveLetter);
                }
            }
            catch
            {
                // Fallback to empty known drives if query fails
            }
        }
    }

    /// <summary>
    /// Scans the system for newly inserted removable drives.
    /// Returns a list of new Incidents for newly arrived drives.
    /// </summary>
    public List<Incident> DetectNewInsertions(string workstationName = "")
    {
        var currentDrives = GetCurrentRemovableDrives();
        return EvaluateDriveChanges(currentDrives, workstationName);
    }

    /// <summary>
    /// Pure, unit-testable evaluation of drive snapshots against internal state.
    /// </summary>
    public List<Incident> EvaluateDriveChanges(IReadOnlyList<UsbDriveSnapshot> currentDrives, string workstationName = "")
    {
        var newIncidents = new List<Incident>();

        lock (_lock)
        {
            var currentLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var drive in currentDrives)
            {
                currentLetters.Add(drive.DriveLetter);

                if (!_knownDriveLetters.Contains(drive.DriveLetter))
                {
                    // Newly inserted drive!
                    _knownDriveLetters.Add(drive.DriveLetter);

                    string friendlyName = !string.IsNullOrWhiteSpace(drive.VolumeLabel)
                        ? $"{drive.VolumeLabel} ({drive.DriveLetter})"
                        : $"USB Drive ({drive.DriveLetter})";

                    var incident = new Incident
                    {
                        IncidentId = Guid.NewGuid(),
                        DetectedAt = DateTime.UtcNow,
                        EventType = SecurityEventType.USBStorageInserted,
                        TargetUser = Environment.UserName,
                        Workstation = string.IsNullOrEmpty(workstationName) ? Environment.MachineName : workstationName,
                        LogonType = 0,
                        CaptureStatus = CaptureStatus.NotApplicable,
                        CapturePath = null,
                        NotificationStatus = NotificationStatus.NotAttempted,
                        DeviceName = friendlyName,
                        DriveLetter = drive.DriveLetter,
                        VolumeLabel = drive.VolumeLabel,
                        Details = $"Removable storage inserted on {drive.DriveLetter}" +
                                  (!string.IsNullOrEmpty(drive.VolumeLabel) ? $" [Volume: {drive.VolumeLabel}]" : "") +
                                  (!string.IsNullOrEmpty(drive.DriveFormat) ? $" [Format: {drive.DriveFormat}]" : "")
                    };

                    newIncidents.Add(incident);
                }
            }

            // Remove drives that were unplugged so future re-insertions will trigger again
            _knownDriveLetters.RemoveWhere(letter => !currentLetters.Contains(letter));
        }

        return newIncidents;
    }

    /// <summary>
    /// Safely polls DriveInfo to find all active removable volumes.
    /// </summary>
    public static List<UsbDriveSnapshot> GetCurrentRemovableDrives()
    {
        var list = new List<UsbDriveSnapshot>();

        try
        {
            var drives = DriveInfo.GetDrives();
            foreach (var d in drives)
            {
                try
                {
                    if (d.DriveType == DriveType.Removable && d.IsReady)
                    {
                        string driveLetter = NormalizeDriveLetter(d.Name);
                        string? volumeLabel = null;
                        string? driveFormat = null;
                        long? totalSize = null;

                        try { volumeLabel = string.IsNullOrWhiteSpace(d.VolumeLabel) ? null : d.VolumeLabel; } catch { }
                        try { driveFormat = d.DriveFormat; } catch { }
                        try { totalSize = d.TotalSize; } catch { }

                        list.Add(new UsbDriveSnapshot(
                            RootDirectory: d.Name,
                            DriveLetter: driveLetter,
                            VolumeLabel: volumeLabel,
                            DriveFormat: driveFormat,
                            TotalSizeInBytes: totalSize
                        ));
                    }
                }
                catch
                {
                    // Drive may be transitioning, locked, or unready; ignore
                }
            }
        }
        catch
        {
            // Suppress system-level DriveInfo query exceptions
        }

        return list;
    }

    public static string NormalizeDriveLetter(string pathOrName)
    {
        if (string.IsNullOrWhiteSpace(pathOrName)) return string.Empty;
        string trimmed = pathOrName.Trim().TrimEnd('\\', '/');
        if (trimmed.Length >= 2 && trimmed[1] == ':')
        {
            return trimmed[..2].ToUpperInvariant();
        }
        return trimmed.ToUpperInvariant();
    }
}

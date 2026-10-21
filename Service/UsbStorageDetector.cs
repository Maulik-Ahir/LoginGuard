using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace LoginGuardService;

public record UsbDriveSnapshot(
    string RootDirectory,
    string DriveLetter,
    string? VolumeLabel,
    string? DriveFormat,
    long? TotalSizeInBytes
);

public record UsbPnpDeviceSnapshot(
    string DeviceId,
    string? Name,
    string? Description,
    string? Manufacturer,
    string? PnpClass,
    string? HardwareId,
    string ClassifiedType,
    string? DriveLetter,
    string? VolumeLabel
);

/// <summary>
/// Detects any USB-connected device that Windows can identify and communicate with,
/// including storage drives, phones, network adapters, HID devices, and IoT/serial devices.
/// Combines Windows PnP device tracking with volume-level removable drive inspection.
/// </summary>
public class UsbStorageDetector
{
    private readonly HashSet<string> _knownDriveLetters = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _knownDeviceIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UsbStorageDetector(IEnumerable<string>? initialDrives = null, IEnumerable<string>? initialDeviceIds = null)
    {
        if (initialDrives != null)
        {
            foreach (var d in initialDrives)
            {
                _knownDriveLetters.Add(NormalizeDriveLetter(d));
            }
        }

        if (initialDeviceIds != null)
        {
            foreach (var id in initialDeviceIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                    _knownDeviceIds.Add(id.Trim());
            }
        }
    }

    /// <summary>
    /// Initializes current devices at service start so already-connected devices don't trigger false alerts.
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
                // Fallback
            }

            try
            {
                var currentDevices = QueryActivePnpUsbDevices();
                _knownDeviceIds.Clear();
                foreach (var dev in currentDevices)
                {
                    _knownDeviceIds.Add(dev.DeviceId);
                }
            }
            catch
            {
                // Fallback
            }
        }
    }

    /// <summary>
    /// Scans the system for newly inserted USB devices and removable drives.
    /// Returns a list of new Incidents for newly arrived devices.
    /// </summary>
    public List<Incident> DetectNewInsertions(string workstationName = "")
    {
        var incidents = new List<Incident>();

        // 1. Detect PnP USB devices (phones, IoT, HID, USB storage adapters, etc.)
        try
        {
            var pnpDevices = QueryActivePnpUsbDevices();
            var pnpIncidents = EvaluatePnpDeviceChanges(pnpDevices, workstationName);
            incidents.AddRange(pnpIncidents);
        }
        catch
        {
            // Graceful fallback if WMI is unavailable
        }

        // 2. Also detect removable storage volumes (assigns drive letters / formats)
        try
        {
            var currentDrives = GetCurrentRemovableDrives();
            var driveIncidents = EvaluateDriveChanges(currentDrives, workstationName);
            
            // Only add drive incidents if they represent a newly arrived drive letter not already detailed by PnP
            foreach (var driveInc in driveIncidents)
            {
                bool alreadyCovered = incidents.Any(i => 
                    !string.IsNullOrEmpty(i.DriveLetter) && 
                    string.Equals(i.DriveLetter, driveInc.DriveLetter, StringComparison.OrdinalIgnoreCase));

                if (!alreadyCovered)
                {
                    incidents.Add(driveInc);
                }
            }
        }
        catch
        {
            // Graceful fallback
        }

        return incidents;
    }

    /// <summary>
    /// Pure, unit-testable evaluation of PnP USB device snapshots against internal state.
    /// </summary>
    public List<Incident> EvaluatePnpDeviceChanges(IReadOnlyList<UsbPnpDeviceSnapshot> currentDevices, string workstationName = "")
    {
        var newIncidents = new List<Incident>();

        lock (_lock)
        {
            var currentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var dev in currentDevices)
            {
                if (string.IsNullOrWhiteSpace(dev.DeviceId)) continue;

                currentIds.Add(dev.DeviceId);

                if (!_knownDeviceIds.Contains(dev.DeviceId))
                {
                    _knownDeviceIds.Add(dev.DeviceId);

                    string deviceName = !string.IsNullOrWhiteSpace(dev.Name)
                        ? dev.Name
                        : (!string.IsNullOrWhiteSpace(dev.Description) ? dev.Description : "Unknown USB Device");

                    var vidPid = ExtractVidPid(dev.HardwareId ?? dev.DeviceId);
                    string vidPidInfo = !string.IsNullOrEmpty(vidPid) ? $" [VID/PID: {vidPid}]" : "";
                    string mfgInfo = !string.IsNullOrWhiteSpace(dev.Manufacturer) ? $" (Mfg: {dev.Manufacturer})" : "";
                    string driveInfo = !string.IsNullOrEmpty(dev.DriveLetter) ? $" — {dev.DriveLetter}" : "";

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
                        DeviceName = deviceName,
                        DeviceDescription = dev.Description,
                        Manufacturer = dev.Manufacturer,
                        DeviceClass = dev.ClassifiedType,
                        HardwareId = dev.HardwareId,
                        DeviceId = dev.DeviceId,
                        DriveLetter = dev.DriveLetter,
                        VolumeLabel = dev.VolumeLabel,
                        Details = $"USB {dev.ClassifiedType} connected: {deviceName}{mfgInfo}{vidPidInfo}{driveInfo}"
                    };

                    newIncidents.Add(incident);
                }
            }

            // Remove disconnected devices so re-insertions trigger again
            _knownDeviceIds.RemoveWhere(id => !currentIds.Contains(id));
        }

        return newIncidents;
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
                        : $"USB Storage Drive ({drive.DriveLetter})";

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
                        DeviceClass = "USB storage",
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
    /// Queries all present PnP USB devices from Windows WMI (Win32_PnPEntity).
    /// Filters out root hubs and motherboard controllers to focus on connected peripherals/devices.
    /// </summary>
    public static List<UsbPnpDeviceSnapshot> QueryActivePnpUsbDevices()
    {
        var devices = new List<UsbPnpDeviceSnapshot>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, Name, Description, Manufacturer, PNPClass, Service, HardwareID, Present " +
                "FROM Win32_PnPEntity WHERE Present = TRUE");

            using var results = searcher.Get();

            foreach (ManagementObject obj in results)
            {
                try
                {
                    string deviceId = obj["DeviceID"]?.ToString() ?? "";
                    string pnpClass = obj["PNPClass"]?.ToString() ?? "";

                    // Match devices that are USB or WPD (phones / portable devices)
                    bool isUsb = deviceId.StartsWith("USB\\", StringComparison.OrdinalIgnoreCase) ||
                                 deviceId.StartsWith("USBSTOR\\", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(pnpClass, "WPD", StringComparison.OrdinalIgnoreCase);

                    if (!isUsb) continue;

                    // Exclude internal root hubs and host controllers
                    if (deviceId.StartsWith("USB\\ROOT_HUB", StringComparison.OrdinalIgnoreCase) ||
                        deviceId.StartsWith("USB\\VID_0000&PID_0000", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(pnpClass, "USBHostController", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string name = obj["Name"]?.ToString() ?? "";
                    string description = obj["Description"]?.ToString() ?? "";
                    string manufacturer = obj["Manufacturer"]?.ToString() ?? "";
                    string service = obj["Service"]?.ToString() ?? "";

                    string? hardwareId = null;
                    if (obj["HardwareID"] is string[] hwIds && hwIds.Length > 0)
                    {
                        hardwareId = hwIds[0];
                    }

                    string classified = ClassifyDeviceType(pnpClass, service, name, description, deviceId);

                    devices.Add(new UsbPnpDeviceSnapshot(
                        DeviceId: deviceId,
                        Name: string.IsNullOrWhiteSpace(name) ? null : name,
                        Description: string.IsNullOrWhiteSpace(description) ? null : description,
                        Manufacturer: string.IsNullOrWhiteSpace(manufacturer) ? null : manufacturer,
                        PnpClass: string.IsNullOrWhiteSpace(pnpClass) ? null : pnpClass,
                        HardwareId: hardwareId,
                        ClassifiedType: classified,
                        DriveLetter: null,
                        VolumeLabel: null
                    ));
                }
                catch
                {
                    // Ignore single device extraction error
                }
            }
        }
        catch
        {
            // WMI service failure fallback
        }

        return devices;
    }

    /// <summary>
    /// Classifies device class/type into: phone, USB storage, network adapter, HID, IoT/serial device, or unknown.
    /// </summary>
    public static string ClassifyDeviceType(string? pnpClass, string? service, string? name, string? description, string? deviceId)
    {
        string combined = $"{pnpClass} {service} {name} {description} {deviceId}".ToLowerInvariant();

        // 1. Phone / Portable Media Player
        if (string.Equals(pnpClass, "WPD", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("phone") || combined.Contains("android") || combined.Contains("iphone") ||
            combined.Contains("mtp") || combined.Contains("ptp") || combined.Contains("portable device"))
        {
            return "phone";
        }

        // 2. USB Storage
        if (string.Equals(pnpClass, "DiskDrive", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(service, "USBSTOR", StringComparison.OrdinalIgnoreCase) ||
            (deviceId != null && deviceId.StartsWith("USBSTOR", StringComparison.OrdinalIgnoreCase)) ||
            combined.Contains("flash drive") || combined.Contains("mass storage") || combined.Contains("card reader"))
        {
            return "USB storage";
        }

        // 3. Network Adapter
        if (string.Equals(pnpClass, "Net", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("ethernet") || combined.Contains("wi-fi") || combined.Contains("wlan") ||
            combined.Contains("802.11") || combined.Contains("rndis") || combined.Contains("network adapter"))
        {
            return "network adapter";
        }

        // 4. HID (Human Interface Device)
        if (string.Equals(pnpClass, "HIDClass", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pnpClass, "Keyboard", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(pnpClass, "Mouse", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("hid") || combined.Contains("keyboard") || combined.Contains("mouse") ||
            combined.Contains("barcode") || combined.Contains("touchscreen") || combined.Contains("gamepad"))
        {
            return "HID";
        }

        // 5. IoT / Serial Device
        if (string.Equals(pnpClass, "Ports", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(service, "usbser", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("com") || combined.Contains("serial") || combined.Contains("uart") ||
            combined.Contains("ch340") || combined.Contains("cp210") || combined.Contains("ftdi") ||
            combined.Contains("arduino") || combined.Contains("raspberry") || combined.Contains("esp32") ||
            combined.Contains("cdc"))
        {
            return "IoT/serial device";
        }

        return "unknown";
    }

    /// <summary>
    /// Extracts VID and PID string from a hardware ID or device ID (e.g. "VID_22D9&PID_2764").
    /// </summary>
    public static string? ExtractVidPid(string? idString)
    {
        if (string.IsNullOrWhiteSpace(idString)) return null;

        var match = Regex.Match(idString, @"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return match.Value.ToUpperInvariant();
        }

        return null;
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

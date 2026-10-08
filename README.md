# LoginGuard

**LoginGuard** is a Windows security utility and background service that monitors failed interactive logon attempts (Windows Security Event ID 4625), captures an evidence photo via the laptop's webcam, delivers instant Telegram alerts with the photo, and allows the authorized owner to remotely lock their laptop using the `/lock` command.

---

## Key Features

- **Automated Intruder Capture**: Detects failed password and PIN attempts (Interactive, Unlock, and Remote Desktop logins) and silently photographs the person in front of the screen.
- **Instant Telegram Alerts**: Sends the evidence capture and timestamped metadata directly to your authorized Telegram chat.
- **Offline Resilience**: Automatically queues notifications locally if internet access is down, and retries with exponential backoff and instant wakeup upon network reconnection.
- **Remote Telegram Interface**: Expand remote interaction from `/lock` into a full monitoring bot supporting `/status`, `/last` (with photo), `/history`, `/pending`, and `/help`.
- **Structured Incident History & UI**: Stores each detected failed logon as structured JSON in a dedicated history repository and provides a comprehensive "Incident History" tab in the UI with filtering, details pane, and direct capture viewing.
- **Local Privacy & Data Retention**: Configurable automated cleanup of older captures, incident history records, and log files. Evidence is stored locally on the machine.
- **Settings & Monitoring UI**: Windows Forms management console for live event logs, incident history, camera testing, Telegram testing, and service lifecycle control.

---

## Architecture & Components

LoginGuard consists of three core components:

1. **`LoginGuardService` (Windows Service)**:
   - Runs in the background under `LocalSystem`.
   - Listens for Windows Event ID 4625 using `EventLogWatcher`.
   - Captures evidence photos via OpenCV (`OpenCvSharp4`).
   - Dispatches alerts and handles Telegram polling for remote status and lock commands.
   - Manages automatic storage retention, history records, and log rotation.

2. **`LoginGuardUI` (Management Application)**:
   - Provides real-time activity log visualization and dedicated structured Incident History exploration.
   - Allows configuration of Telegram credentials, camera index, and retention policies.
   - Features built-in hardware and network diagnostics.
   - Controls Windows Service lifecycle (Start, Stop, Restart).

3. **`LoginGuardSetup` (Inno Setup Installer)**:
   - 64-bit installer for automated service registration and credential setup.

---

## File & Data Locations

| Directory / File | Description |
| :--- | :--- |
| `C:\CameraSpikeLog\service_log.txt` | Service activity and diagnostics log. |
| `C:\CameraSpikeLog\Captures\` | Stored webcam evidence photos (`.jpg`). |
| `C:\CameraSpikeLog\PendingNotifications\` | Temporary queue for unsent notifications during network outages. |
| `C:\CameraSpikeLog\History\` | Structured JSON repository of all recorded login incidents. |
| `C:\Program Files\LoginGuard\Service\appsettings.Secrets.json` | Production configuration and Telegram credentials (admin protected). |

---

## Configuration (`appsettings.Secrets.json`)

```json
{
  "Telegram": {
    "BotToken": "YOUR_TELEGRAM_BOT_TOKEN",
    "ChatId": "YOUR_TELEGRAM_CHAT_ID"
  },
  "Camera": {
    "DeviceIndex": 0
  },
  "Storage": {
    "CaptureRetentionDays": 30,
    "LogRetentionDays": 15
  }
}
```

- `CaptureRetentionDays`: Days to retain evidence captures and incident history (`7`, `15`, `30`, `60`, or `-1` for Never).
- `LogRetentionDays`: Days to retain service logs (`7`, `15`, `30`, or `90`).
- `DeviceIndex`: 0-indexed webcam device ID.

---

## System Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit)
- **Runtime**: .NET 10.0 Runtime (Windows Desktop & Core)
- **Hardware**: Integrated or USB webcam
- **Permissions**: Administrator privileges (for Windows Service registration and EventLog auditing)

---

## Building from Source

To build all projects:

```cmd
dotnet build LoginGuard.slnx
```

To run automated unit and integration tests:

```cmd
dotnet test Tests/ConfigManagerTests/ConfigManagerTests.csproj
```

---

## Version History

- **v0.8.0**: Incident History & Telegram expansion:
  - Persistent, file-based structured incident JSON repository in `C:\CameraSpikeLog\History\`.
  - Dedicated Incident History UI tab with table view, metadata inspection, filter presets, and capture viewer.
  - Expanded Telegram commands: `/help`, `/status`, `/last` (with photo playback), `/history`, `/pending`, and preserved Session 0 `/lock`.
  - Automatic retention policy enforcement for historical incident records.
- **v0.7.0**: Major reliability, security, and production-readiness pass. Hardened EventRecord handle lifecycle, unblocked pending notification queue, camera auto-exposure warmup, unmanaged memory safety in remote lock, xUnit test suite migration, and async UI responsiveness.
- **v0.6.0**: Dynamic configuration presets, Inno Setup 6 x64 installer with secrets wizard, and Session 0 workstation lock.

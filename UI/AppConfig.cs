namespace LoginGuardUI;

public class AppConfig
{
    public TelegramConfig Telegram { get; set; } = new();
    public CameraConfig Camera { get; set; } = new();
    public StorageConfig Storage { get; set; } = new();
}

public class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
}

public class CameraConfig
{
    public int DeviceIndex { get; set; } = 0;
}

public class StorageConfig
{
    public int CaptureRetentionDays { get; set; } = 30; // Allowed presets: 7, 15, 30, 60, -1 (Never)
    public int LogRetentionDays { get; set; } = 15;     // Allowed presets: 7, 15, 30, 90
}

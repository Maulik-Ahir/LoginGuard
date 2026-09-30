using System.Text.Json;

namespace LoginGuardUI;

public static class ConfigManager
{
    public static readonly int[] AllowedCaptureRetentionPresets = [7, 15, 30, 60, -1];
    public static readonly int[] AllowedLogRetentionPresets = [7, 15, 30, 90];

    public static string GetConfigFilePath()
    {
        // 1. Check relative to UI directory in standard production install: ../Service/appsettings.Secrets.json
        string appDir = AppContext.BaseDirectory;
        string installedPath = Path.GetFullPath(Path.Combine(appDir, "..", "Service", "appsettings.Secrets.json"));
        if (File.Exists(installedPath))
        {
            return installedPath;
        }

        // 2. Check development environment relative paths (from bin/Debug/...)
        string devServiceDir = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "Service"));
        if (Directory.Exists(devServiceDir))
        {
            return Path.Combine(devServiceDir, "appsettings.Secrets.json");
        }

        // 3. Check workspace development path
        string workspaceServiceDir = @"C:\antigrav\Service";
        if (Directory.Exists(workspaceServiceDir))
        {
            return Path.Combine(workspaceServiceDir, "appsettings.Secrets.json");
        }

        // 4. Check standard Program Files production location
        string defaultProdPath = @"C:\Program Files\LoginGuard\Service\appsettings.Secrets.json";
        if (File.Exists(defaultProdPath))
        {
            return defaultProdPath;
        }

        // Default to installed relative path
        return installedPath;
    }

    public static AppConfig Load()
    {
        string filePath = GetConfigFilePath();
        if (!File.Exists(filePath))
        {
            return new AppConfig();
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };

            var config = JsonSerializer.Deserialize<AppConfig>(json, options) ?? new AppConfig();

            // Ensure nested objects are non-null
            config.Telegram ??= new TelegramConfig();
            config.Camera ??= new CameraConfig();
            config.Storage ??= new StorageConfig();

            // Validate and sanitize camera index
            if (config.Camera.DeviceIndex < 0)
            {
                config.Camera.DeviceIndex = 0;
            }

            // Validate and sanitize presets with defaults on missing/corrupted values
            if (!AllowedCaptureRetentionPresets.Contains(config.Storage.CaptureRetentionDays))
            {
                config.Storage.CaptureRetentionDays = 30;
            }

            if (!AllowedLogRetentionPresets.Contains(config.Storage.LogRetentionDays))
            {
                config.Storage.LogRetentionDays = 15;
            }

            return config;
        }
        catch
        {
            // On parse failure or I/O error, return safe defaults rather than crashing
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        config.Telegram ??= new TelegramConfig();
        config.Camera ??= new CameraConfig();
        config.Storage ??= new StorageConfig();

        // Validation 1: Camera device index validation
        if (config.Camera.DeviceIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(config.Camera.DeviceIndex),
                "Camera device index must be greater than or equal to 0.");
        }

        // Validation 2: Retention presets validation
        if (!AllowedCaptureRetentionPresets.Contains(config.Storage.CaptureRetentionDays))
        {
            throw new ArgumentOutOfRangeException(nameof(config.Storage.CaptureRetentionDays),
                $"Capture retention must be one of the allowed presets: {string.Join(", ", AllowedCaptureRetentionPresets)}");
        }

        if (!AllowedLogRetentionPresets.Contains(config.Storage.LogRetentionDays))
        {
            throw new ArgumentOutOfRangeException(nameof(config.Storage.LogRetentionDays),
                $"Log retention must be one of the allowed presets: {string.Join(", ", AllowedLogRetentionPresets)}");
        }

        // Validation 3: Telegram credentials validation
        string token = config.Telegram.BotToken?.Trim() ?? string.Empty;
        string chatId = config.Telegram.ChatId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Telegram Bot Token cannot be empty.", nameof(config));
        }

        if (!token.Contains(':') || token.Length < 20)
        {
            throw new ArgumentException("Telegram Bot Token format appears invalid. It should follow the format '123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ'.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(chatId))
        {
            throw new ArgumentException("Telegram Chat ID cannot be empty.", nameof(config));
        }

        if (!long.TryParse(chatId, out _))
        {
            throw new ArgumentException("Telegram Chat ID must be a numeric ID (e.g. 5635942580 or -100123456789).", nameof(config));
        }

        config.Telegram.BotToken = token;
        config.Telegram.ChatId = chatId;

        string targetPath = GetConfigFilePath();
        string? targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Atomic write pattern: write to .tmp file first, then atomic move
        string tempPath = targetPath + $".tmp_{Guid.NewGuid():N}";
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(tempPath, json);

            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best effort */ }
            }
        }
    }
}

using System.Text.Json;

namespace LoginGuardUI;

public static class ConfigManager
{
    public static readonly int[] AllowedCaptureRetentionPresets = [7, 15, 30, 60, -1];
    public static readonly int[] AllowedLogRetentionPresets = [7, 15, 30, 90];

    public static string GetConfigFilePath()
    {
        // 1. Check relative to UI directory in standard install: ../Service/appsettings.Secrets.json
        string appDir = AppContext.BaseDirectory;
        string installedPath = Path.GetFullPath(Path.Combine(appDir, "..", "Service", "appsettings.Secrets.json"));
        if (File.Exists(installedPath))
        {
            return installedPath;
        }

        // 2. Check development environment relative paths
        string devRelativePath = Path.GetFullPath(Path.Combine(appDir, "..", "..", "..", "..", "Service", "appsettings.Secrets.json"));
        if (File.Exists(devRelativePath))
        {
            return devRelativePath;
        }

        // 3. Check fixed workspace path if developing in c:\antigrav
        string workspacePath = @"C:\antigrav\Service\appsettings.Secrets.json";
        if (File.Exists(workspacePath))
        {
            return workspacePath;
        }

        // 4. Check standard Program Files production location
        string defaultProdPath = @"C:\Program Files\LoginGuard\Service\appsettings.Secrets.json";
        if (File.Exists(defaultProdPath))
        {
            return defaultProdPath;
        }

        // Default to the installed relative path (will be created if saved)
        return Directory.Exists(Path.GetDirectoryName(installedPath)) ? installedPath : workspacePath;
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
            // On parse failure, return safe defaults rather than crashing
            return new AppConfig();
        }
    }

    public static void Save(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Validation 1: Retention presets validation
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

        // Validation 2: Telegram credentials validation
        string token = config.Telegram.BotToken?.Trim() ?? string.Empty;
        string chatId = config.Telegram.ChatId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Telegram Bot Token cannot be empty.", nameof(config));
        }

        if (string.IsNullOrWhiteSpace(chatId))
        {
            throw new ArgumentException("Telegram Chat ID cannot be empty.", nameof(config));
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

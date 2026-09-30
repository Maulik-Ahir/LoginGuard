using LoginGuardUI;
using Xunit;

namespace LoginGuard.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly string _configFilePath;
    private readonly string? _originalSecretsBackup;

    public ConfigManagerTests()
    {
        _configFilePath = ConfigManager.GetConfigFilePath();
        if (File.Exists(_configFilePath))
        {
            _originalSecretsBackup = File.ReadAllText(_configFilePath);
        }
    }

    public void Dispose()
    {
        try
        {
            if (_originalSecretsBackup != null)
            {
                File.WriteAllText(_configFilePath, _originalSecretsBackup);
            }
            else if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
        }
        catch
        {
            // Best effort cleanup/restore
        }
    }

    [Fact]
    public void Save_NullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ConfigManager.Save(null!));
    }

    [Theory]
    [InlineData(45)]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100)]
    public void Save_InvalidCaptureRetentionPreset_ThrowsArgumentOutOfRangeException(int invalidDays)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = "12345678" },
            Storage = new StorageConfig { CaptureRetentionDays = invalidDays, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => ConfigManager.Save(config));
    }

    [Theory]
    [InlineData(20)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(60)]
    public void Save_InvalidLogRetentionPreset_ThrowsArgumentOutOfRangeException(int invalidDays)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = "12345678" },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = invalidDays }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => ConfigManager.Save(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_EmptyOrWhitespaceBotToken_ThrowsArgumentException(string invalidToken)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = invalidToken, ChatId = "12345678" },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentException>(() => ConfigManager.Save(config));
    }

    [Theory]
    [InlineData("invalid_token_no_colon")]
    [InlineData("short:tok")]
    public void Save_MalformedBotToken_ThrowsArgumentException(string malformedToken)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = malformedToken, ChatId = "12345678" },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentException>(() => ConfigManager.Save(config));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Save_EmptyOrWhitespaceChatId_ThrowsArgumentException(string invalidChatId)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = invalidChatId },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentException>(() => ConfigManager.Save(config));
    }

    [Theory]
    [InlineData("not_a_number")]
    [InlineData("@username")]
    public void Save_NonNumericChatId_ThrowsArgumentException(string nonNumericChatId)
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = nonNumericChatId },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentException>(() => ConfigManager.Save(config));
    }

    [Fact]
    public void Save_NegativeCameraDeviceIndex_ThrowsArgumentOutOfRangeException()
    {
        var config = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = "12345678" },
            Camera = new CameraConfig { DeviceIndex = -1 },
            Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => ConfigManager.Save(config));
    }

    [Fact]
    public void SaveAndLoad_ValidConfig_RoundtripsSuccessfully()
    {
        var validConfig = new AppConfig
        {
            Telegram = new TelegramConfig { BotToken = "123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", ChatId = "5635942580" },
            Camera = new CameraConfig { DeviceIndex = 1 },
            Storage = new StorageConfig { CaptureRetentionDays = -1, LogRetentionDays = 90 }
        };

        ConfigManager.Save(validConfig);
        var loaded = ConfigManager.Load();

        Assert.Equal("123456789:ABCDefGhIJKlmNoPQRsTUVwxyZ", loaded.Telegram.BotToken);
        Assert.Equal("5635942580", loaded.Telegram.ChatId);
        Assert.Equal(1, loaded.Camera.DeviceIndex);
        Assert.Equal(-1, loaded.Storage.CaptureRetentionDays);
        Assert.Equal(90, loaded.Storage.LogRetentionDays);
    }

    [Fact]
    public void Load_DefaultAppConfig_HasExpectedDefaults()
    {
        var config = new AppConfig();

        Assert.NotNull(config.Telegram);
        Assert.NotNull(config.Camera);
        Assert.NotNull(config.Storage);
        Assert.Equal(0, config.Camera.DeviceIndex);
        Assert.Equal(30, config.Storage.CaptureRetentionDays);
        Assert.Equal(15, config.Storage.LogRetentionDays);
    }
}

using LoginGuardUI;

Console.WriteLine("=== Testing ConfigManager & AppConfig (.NET 10 Runtime) ===");

// Test 1: Invalid Capture Retention Preset rejection
bool test1Passed = false;
try
{
    var invalidConfig = new AppConfig
    {
        Telegram = new TelegramConfig { BotToken = "valid_token", ChatId = "12345" },
        Storage = new StorageConfig { CaptureRetentionDays = 45, LogRetentionDays = 15 } // 45 is invalid
    };
    ConfigManager.Save(invalidConfig);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("FAIL: CaptureRetentionDays=45 was not rejected.");
    Console.ResetColor();
}
catch (ArgumentOutOfRangeException ex)
{
    test1Passed = true;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"PASS: Correctly rejected invalid capture retention preset (45 days): {ex.Message}");
    Console.ResetColor();
}

// Test 2: Invalid Log Retention Preset rejection
bool test2Passed = false;
try
{
    var invalidConfig = new AppConfig
    {
        Telegram = new TelegramConfig { BotToken = "valid_token", ChatId = "12345" },
        Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 20 } // 20 is invalid
    };
    ConfigManager.Save(invalidConfig);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("FAIL: LogRetentionDays=20 was not rejected.");
    Console.ResetColor();
}
catch (ArgumentOutOfRangeException ex)
{
    test2Passed = true;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"PASS: Correctly rejected invalid log retention preset (20 days): {ex.Message}");
    Console.ResetColor();
}

// Test 3: Empty Bot Token rejection
bool test3Passed = false;
try
{
    var invalidConfig = new AppConfig
    {
        Telegram = new TelegramConfig { BotToken = "   ", ChatId = "12345" },
        Storage = new StorageConfig { CaptureRetentionDays = 30, LogRetentionDays = 15 }
    };
    ConfigManager.Save(invalidConfig);
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("FAIL: Whitespace bot token was not rejected.");
    Console.ResetColor();
}
catch (ArgumentException ex)
{
    test3Passed = true;
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"PASS: Correctly rejected empty/whitespace bot token: {ex.Message}");
    Console.ResetColor();
}

// Test 4: Valid Presets, Atomic Save, and Load Verification
var validConfig = new AppConfig
{
    Telegram = new TelegramConfig { BotToken = "test_bot_token_abc", ChatId = "78910" },
    Camera = new CameraConfig { DeviceIndex = 1 },
    Storage = new StorageConfig { CaptureRetentionDays = -1, LogRetentionDays = 90 }
};

ConfigManager.Save(validConfig);
var loadedConfig = ConfigManager.Load();

bool test4Passed = loadedConfig.Telegram.BotToken == "test_bot_token_abc" &&
                   loadedConfig.Telegram.ChatId == "78910" &&
                   loadedConfig.Camera.DeviceIndex == 1 &&
                   loadedConfig.Storage.CaptureRetentionDays == -1 &&
                   loadedConfig.Storage.LogRetentionDays == 90;

if (test4Passed)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("PASS: Atomic save and load preserved all fields, including Never (-1) capture retention preset.");
    Console.ResetColor();
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("FAIL: Loaded config values did not match saved config.");
    Console.ResetColor();
}

// Clean up test file so workspace stays clean
string savedPath = ConfigManager.GetConfigFilePath();
if (File.Exists(savedPath))
{
    File.Delete(savedPath);
    Console.WriteLine($"Test cleanup: deleted temporary secrets file at {savedPath}");
}

if (test1Passed && test2Passed && test3Passed && test4Passed)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("\n>>> ALL TESTS PASSED SUCCESSFULLY! <<<");
    Console.ResetColor();
    Environment.Exit(0);
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("\n>>> ONE OR MORE TESTS FAILED. <<<");
    Console.ResetColor();
    Environment.Exit(1);
}

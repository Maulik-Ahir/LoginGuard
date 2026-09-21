using LoginGuardService;

var builder = Host.CreateApplicationBuilder(args);

// Windows Service configuration (runs as LocalSystem in Session 0)
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "LoginGuardService";
});

// Explicit base path to ensure config files resolve correctly when launched by SCM
string baseDir = AppContext.BaseDirectory;
builder.Configuration.SetBasePath(baseDir);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: false);

builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

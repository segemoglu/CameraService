using CameraService.Features;
using CameraService.Features.SnapshotCapture;
using Serilog;
using System.Net;

var builder = Host.CreateApplicationBuilder(args);

// Ayarlarý al
var config = builder.Configuration;
var username = config["CameraSettings:Username"];
var password = config["CameraSettings:Password"];
var logPath = config["StorageSettings:LogPath"] ?? @"C:\logs.txt"; // Dosyadan log yolunu oku

// Serilog Ayarý
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(logPath, // Hardcoded yol yerine deðiþkeni kullan    fileSizeLimitBytes: 10 * 1024 * 1024,
    rollOnFileSizeLimit: true,
    retainedFileCountLimit: 7,
    rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CameraService";
});

// HttpClient ve Kimlik Doðrulama
builder.Services.AddHttpClient<ICameraClient, CameraClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Credentials = new NetworkCredential(username, password)
    });

// VSA Özellik Baðýmlýlýklarý
builder.Services.AddSingleton<IImageStorage, LocalImageStorage>();
builder.Services.AddSingleton<SnapshotCaptureHandler>();
builder.Services.AddHostedService<DatabaseTriggerWorker>();

var host = builder.Build();
host.Run();
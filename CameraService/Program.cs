using CameraService.Features;
using CameraService.Workers;
using System.Net;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog Ayarý (Loglarý txt dosyasýna yazar)
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(@"C:\Users\it.stajyer\Desktop\KameraServisi\logs.txt", retainedFileCountLimit: 30, rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CameraService";
});

// HttpClient ve NetworkCredential (Sadece 1 kere yapýlandýrýlýr)
builder.Services.AddHttpClient<ICameraClient, CameraClient>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        Credentials = new NetworkCredential("YOUR_CAMERA_USERNAME", "YOUR_CAMERA_PASSWORD")
    });

// Feature Sýnýflarýnýn Kayýtlarý
builder.Services.AddSingleton<IImageStorage, LocalImageStorage>();
builder.Services.AddSingleton<SnapshotCaptureHandler>();

// Worker Kaydý
builder.Services.AddHostedService<DatabaseTriggerWorker>();

var host = builder.Build();
host.Run();
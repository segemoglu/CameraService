using CameraService.Features;
using CameraService.Workers;
using System.Net;

var builder = Host.CreateApplicationBuilder(args);

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
using Microsoft.Extensions.Configuration;

namespace CameraService.Features.SnapshotCapture
{
    public class LocalImageStorage : IImageStorage
    {
        private readonly ILogger<LocalImageStorage> _logger;
        private readonly string _baseFolderPath;

        public LocalImageStorage(ILogger<LocalImageStorage> logger, IConfiguration config)
        {
            _logger = logger;
            _baseFolderPath = config["StorageSettings:BasePath"] ?? @"C:\Fotoğraflar";
        }

        public async Task SaveImageAsync(byte[] imageBytes, string barcode)
        {
            DateTime now = DateTime.Now;
            string targetFolderPath = Path.Combine(_baseFolderPath, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));
            Directory.CreateDirectory(targetFolderPath);

            string fileName = Path.Combine(targetFolderPath, $"{barcode}_Station26-TopCamera_{now:dd-MM-yyyy HH-mm-ss}.jpg");
            await File.WriteAllBytesAsync(fileName, imageBytes);
            _logger.LogInformation($"Snapshot başarıyla kaydedildi: {fileName}");
        }
    }
}
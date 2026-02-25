using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraService.Features
{
    public class LocalImageStorage : IImageStorage
    {
        private readonly ILogger<LocalImageStorage> _logger;
        private readonly string _baseFolderPath = @"C:\Users\it.stajyer\Desktop\Fotoğraflar";

        public LocalImageStorage(ILogger<LocalImageStorage> logger)
        {
            _logger = logger;
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

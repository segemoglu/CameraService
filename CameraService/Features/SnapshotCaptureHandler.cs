using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraService.Features
{
    public class SnapshotCaptureHandler
    {
        private readonly ICameraClient _cameraClient;
        private readonly IImageStorage _imageStorage;

        public SnapshotCaptureHandler(ICameraClient cameraClient, IImageStorage imageStorage)
        {
            _cameraClient = cameraClient;
            _imageStorage = imageStorage;
        }

        public async Task HandleAsync(string barcode)
        {
            // 1. Kameradan fotoğrafı al
            byte[]? imageBytes = await _cameraClient.GetSnapshotBytesAsync();

            // 2. Başarılıysa diske kaydet
            if (imageBytes != null && imageBytes.Length > 0)
            {
                await _imageStorage.SaveImageAsync(imageBytes, barcode);
            }
        }
    }
}

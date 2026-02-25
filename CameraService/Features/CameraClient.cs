using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraService.Features
{
    public class CameraClient : ICameraClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CameraClient> _logger;

        public CameraClient(HttpClient httpClient, ILogger<CameraClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<byte[]?> GetSnapshotBytesAsync()
        {
            string url = "http://YOUR_CAMERA_IP/cgi-bin/snapshot.cgi";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsByteArrayAsync();
                }
                _logger.LogWarning($"Kamera hatası: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kamera Bağlantı Hatası: {ex.Message}");
            }
            return null;
        }
    }
}

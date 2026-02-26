using Microsoft.Extensions.Configuration;

namespace CameraService.Features.SnapshotCapture
{
    public class CameraClient : ICameraClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CameraClient> _logger;
        private readonly string _snapshotUrl;

        public CameraClient(HttpClient httpClient, ILogger<CameraClient> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _logger = logger;
            _snapshotUrl = config["CameraSettings:SnapshotUrl"] ?? "";
        }

        public async Task<byte[]?> GetSnapshotBytesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_snapshotUrl);
                if (response.IsSuccessStatusCode) return await response.Content.ReadAsByteArrayAsync();

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
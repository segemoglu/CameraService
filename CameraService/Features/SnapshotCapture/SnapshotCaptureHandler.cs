using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CameraService.Features.SnapshotCapture
{
    public class SnapshotCaptureHandler
    {
        private readonly ICameraClient _cameraClient;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<SnapshotCaptureHandler> _logger;
        private readonly string _connectionString;

        public SnapshotCaptureHandler(ICameraClient cameraClient, IImageStorage imageStorage, ILogger<SnapshotCaptureHandler> logger, IConfiguration config)
        {
            _cameraClient = cameraClient;
            _imageStorage = imageStorage;
            _logger = logger;
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
        }

        public async Task HandleAsync(int id, string barcode)
        {
            try
            {
                // 1. Veritabanına pc_in = 1 yaz
                await DurumGuncelleAsync(id, "pc_in", 1);
                _logger.LogInformation($"[{id}] pc_in = 1 olarak güncellendi.");

                // 2. Kameradan fotoğrafı al
                byte[]? imageBytes = await _cameraClient.GetSnapshotBytesAsync();

                // 3. Başarılıysa diske kaydet ve pc_out = 1 yaz
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    await _imageStorage.SaveImageAsync(imageBytes, barcode);

                    await DurumGuncelleAsync(id, "pc_out", 1);
                    _logger.LogInformation($"[{id}] Fotoğraf kaydedildi ve pc_out = 1 yapıldı.");
                }
                else
                {
                    _logger.LogWarning($"[{id}] Fotoğraf alınamadığı için pc_out güncellenmedi.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{id}] Akış Hatası: {ex.Message}");
            }
        }

        private async Task DurumGuncelleAsync(int id, string column, int value)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                string sql = $"UPDATE dbo.BarcodeTracking SET {column} = @val WHERE Id = @id";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@val", value);
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
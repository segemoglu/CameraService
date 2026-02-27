using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CameraService.Features.SnapshotCapture
{
    public class SnapshotCaptureHandler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ICameraClient _cameraClient;
        private readonly IImageStorage _imageStorage;
        private readonly ILogger<SnapshotCaptureHandler> _logger;

        public SnapshotCaptureHandler(IServiceScopeFactory scopeFactory, ICameraClient cameraClient, IImageStorage imageStorage, ILogger<SnapshotCaptureHandler> logger)
        {
            _scopeFactory = scopeFactory;
            _cameraClient = cameraClient;
            _imageStorage = imageStorage;
            _logger = logger;
        }

        public async Task HandleAsync(int id, string barcode)
        {
            try
            {
                // 1. ADIM: İşlemi Kilitle (pc_in = true yap)
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var kayit = await db.BarcodeTracking.FindAsync(id);
                    if (kayit != null)
                    {
                        kayit.pc_in = true;
                        await db.SaveChangesAsync();
                        _logger.LogInformation($"[{id}] EF Core: pc_in = 1 yapıldı.");
                    }
                }

                // 2. ADIM: Ağ ve Disk I/O (Veritabanı kilitli değil, güvenli bölge)
                byte[]? imageBytes = await _cameraClient.GetSnapshotBytesAsync();

                if (imageBytes != null && imageBytes.Length > 0)
                {
                    await _imageStorage.SaveImageAsync(imageBytes, barcode);

                    // 3. ADIM: Başarılıysa İşi Bitir (pc_out = true yap)
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var kayit = await db.BarcodeTracking.FindAsync(id);
                        if (kayit != null)
                        {
                            kayit.pc_out = true;
                            await db.SaveChangesAsync();
                            _logger.LogInformation($"[{id}] EF Core: Fotoğraf diske kaydedildi ve pc_out = 1 yapıldı.");
                        }
                    }
                }
                else
                {
                    // Kamera boş dönerse zorla hata fırlat ki CATCH bloğu çalışsın
                    throw new Exception("Kameradan veri alınamadı!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[{id}] HATA: {ex.Message} -> İşlem geri alınıyor (Rollback)!");

                // HATA DURUMU (Rollback): İş verenin isteği üzerine her şeyi 0'a (false) çekiyoruz
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var kayit = await db.BarcodeTracking.FindAsync(id);
                    if (kayit != null)
                    {
                        kayit.pc_in = false;
                        kayit.pc_out = false;
                        await db.SaveChangesAsync();
                        _logger.LogWarning($"[{id}] EF Core: Kayıtlar sıfırlandı.");
                    }
                }
            }
        }
    }
}
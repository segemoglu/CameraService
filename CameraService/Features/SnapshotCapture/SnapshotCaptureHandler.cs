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
            bool islemBasarili = false;

            while (!islemBasarili)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var kayit = await db.BarcodeTracking.FindAsync(id);

                        if (kayit != null)
                        {
                            kayit.pc_in = true;

                            // Kameraya bağlanmayı dene
                            byte[]? imageBytes = await _cameraClient.GetSnapshotBytesAsync();

                            if (imageBytes == null || imageBytes.Length == 0)
                            {
                                throw new Exception("Kameradan veri alınamadı!");
                            }

                            await _imageStorage.SaveImageAsync(imageBytes, barcode);

                            
                            kayit.pc_out = true;
                            await db.SaveChangesAsync();

                            islemBasarili = true; // Başarılıysa döngüden çıkmak için true yap
                            _logger.LogInformation($"[{id}] İşlem başarılı: pc_in ve pc_out DB'ye yazıldı.");
                        }
                        else
                        {
                            // Kayıt veritabanında yoksa boşuna denememek için döngüyü kır
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[{id}] HATA: {ex.Message} -> Veritabanına hiçbir şey yazılmadı (İşlem iptal).");

                    await Task.Delay(1000);// Kamerayı boğmamak için 1 saniye nefes payı
                }
            }
        }
    }
}
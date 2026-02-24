using System.Net;
using System.Net.Http;

namespace CameraService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;

            // HttpClient program çalýþtýðýnda SADECE 1 KERE ayaða kalkar
            var handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("YOUR_CAMERA_USERNAME", "YOUR_CAMERA_PASSWORD")
            };
            _httpClient = new HttpClient(handler);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // TODO: Burada veritabanýný kontrol edeceðiz (isTriggered ve barcode alacaðýz)
                bool isTriggered = true; // Þimdilik sahte deðer
                string barcode = "12345";

                if (isTriggered)
                {
                    // Sadece tetiklendiðinde fotoðraf çek
                    await GetSnapshotAsync();

                    // TODO: Veritabanýný 0'a çek
                }

                await Task.Delay(5000, stoppingToken);
            }
        }


        private async Task GetSnapshotAsync()
        {
            string url = "http://YOUR_CAMERA_IP/cgi-bin/snapshot.cgi";
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // Dosya ismini barkod ve anlýk saat ile benzersiz yapýyoruz
                    string folderPath = @"C:\Users\it.stajyer\Desktop\Fotoðraflar";
                    string fileName = Path.Combine(folderPath, $"{DateTime.Now:HH-mm-ss}.jpg");

                    await File.WriteAllBytesAsync(fileName, imageBytes);
                    _logger.LogInformation($"Snapshot baþarýyla kaydedildi: {fileName}");
                }
                else
                {
                    _logger.LogWarning($"Hata: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Kamera Baðlantý Hatasý: {ex.Message}");
            }
        }
    }
}

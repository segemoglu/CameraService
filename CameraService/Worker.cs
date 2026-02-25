using Microsoft.Data.SqlClient;
using System.Net;

namespace CameraService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;Integrated Security=true;TrustServerCertificate=True;";

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            var handler = new HttpClientHandler()
            {
                Credentials = new NetworkCredential("YOUR_CAMERA_USERNAME", "YOUR_CAMERA_PASSWORD")
            };
            _httpClient = new HttpClient(handler);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Servis baþlarken dinlemeyi baþlat
            SqlDependency.Start(_connectionString);
            DinlemeyeKayitOl();
            _logger.LogInformation("BarcodeTracking tablosu dinleniyor...");

            // Sonsuz bekleme: Olay (Event) tabanlý çalýþtýðýmýz için döngüye gerek yok, servisi açýk tutar.
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            // Servis durduðunda dinlemeyi kapat
            SqlDependency.Stop(_connectionString);
            await base.StopAsync(stoppingToken);
        }

        private void DinlemeyeKayitOl()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                // Sütun isimleri güncellendi: plc_in, plc_out
                string sql = "SELECT Id, barcode_no, plc_in, plc_out, pc_in, pc_out, InsertTime FROM dbo.BarcodeTracking";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    SqlDependency dependency = new SqlDependency(cmd);
                    dependency.OnChange += Dependency_OnChange;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                    }
                }
            }
        }

        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (e.Type == SqlNotificationType.Change)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // Sütun ismi güncellendi: plc_in
                    string sql = "SELECT TOP 1 plc_in, barcode_no FROM dbo.BarcodeTracking ORDER BY Id DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            // Deðiþken ve okuma ismi güncellendi: plc_in
                            bool plc_in = reader["plc_in"] != DBNull.Value && (bool)reader["plc_in"];
                            string barcode = reader["barcode_no"]?.ToString() ?? "Bilinmeyen";

                            if (plc_in) // Eðer 1 (True) ise fotoðraf çek
                            {
                                _logger.LogInformation($"plc_in deðeri 1! Snapshot baþlatýlýyor. Barkod: {barcode}");

                                // Metodu tetikler ve bekletmeden çalýþmasýný saðlar
                                _ = GetSnapshotAsync(barcode);
                            }
                        }
                    }
                }
            }

            // Bir sonraki deðiþikliði yakalamak için yeniden kayýt ol
            DinlemeyeKayitOl();
        }

        private async Task GetSnapshotAsync(string barcode)
        {
            string url = "http://YOUR_CAMERA_IP/cgi-bin/snapshot.cgi";
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                    // 1. Ana hedef klasör
                    string baseFolderPath = @"C:\Users\it.stajyer\Desktop\Fotoðraflar";

                    // 2. O anki zamaný al
                    DateTime now = DateTime.Now;

                    // 3. Ýç içe klasör yolunu birleþtir (Örn: ...\Fotoðraflar\2026\02\25)
                    string targetFolderPath = Path.Combine(baseFolderPath, now.ToString("yyyy"), now.ToString("MM"), now.ToString("dd"));

                    // 4. Klasör yapýsý yoksa oluþtur (Varsa hiçbir þey yapmaz, güvenlidir)
                    Directory.CreateDirectory(targetFolderPath);

                    // 5. Dosya adýný sadece saat bilgisiyle oluþtur ve tam yolu belirle
                    string fileName = Path.Combine(targetFolderPath, $"{now:HH-mm-ss}.jpg");

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
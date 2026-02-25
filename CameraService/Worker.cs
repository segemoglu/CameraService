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
                string sql = "SELECT Id, barcode_no, pcl_in, pcl_out, pc_in, pc_out, InsertTime FROM dbo.BarcodeTracking";

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
                    // Fotoðraf ismi için barkodu da (barcode_no) sorguya ekledim
                    string sql = "SELECT TOP 1 pcl_in, barcode_no FROM dbo.BarcodeTracking ORDER BY Id DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool pcl_in = reader["pcl_in"] != DBNull.Value && (bool)reader["pcl_in"];
                            string barcode = reader["barcode_no"]?.ToString() ?? "Bilinmeyen";

                            if (pcl_in) // Eðer 1 (True) ise fotoðraf çek
                            {
                                _logger.LogInformation($"pcl_in deðeri 1! Snapshot baþlatýlýyor. Barkod: {barcode}");

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

                    string folderPath = @"C:\Users\it.stajyer\Desktop\Fotoðraflar";

                    // Dosya ismini Barkod_SaatDakikaSaniye.jpg formatýnda kaydeder
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
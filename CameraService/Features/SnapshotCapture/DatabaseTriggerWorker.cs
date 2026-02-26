using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CameraService.Features.SnapshotCapture
{
    public class DatabaseTriggerWorker : BackgroundService
    {
        private readonly ILogger<DatabaseTriggerWorker> _logger;
        private readonly SnapshotCaptureHandler _handler;
        private readonly string _connectionString;

        public DatabaseTriggerWorker(ILogger<DatabaseTriggerWorker> logger, SnapshotCaptureHandler handler, IConfiguration config)
        {
            _logger = logger;
            _handler = handler;
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SqlDependency.Start(_connectionString);
            DinlemeyeKayitOl();
            _logger.LogInformation("BarcodeTracking tablosu dinleniyor...");
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            SqlDependency.Stop(_connectionString);
            await base.StopAsync(stoppingToken);
        }

        private void DinlemeyeKayitOl()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                string sql = "SELECT Id, barcode_no, plc_in, plc_out, pc_in, pc_out, InsertTime FROM dbo.BarcodeTracking";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    SqlDependency dependency = new SqlDependency(cmd);
                    dependency.OnChange += Dependency_OnChange;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void Dependency_OnChange(object sender, SqlNotificationEventArgs e)
        {
            if (sender is SqlDependency dependency) dependency.OnChange -= Dependency_OnChange;

            if (e.Type == SqlNotificationType.Change && e.Info != SqlNotificationInfo.Invalid)
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    string sql = "SELECT TOP 1 Id, plc_in, pc_in, barcode_no FROM dbo.BarcodeTracking ORDER BY Id DESC";
                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool plc_in = reader["plc_in"] != DBNull.Value && (bool)reader["plc_in"];

                            bool pc_in_durumu = reader["pc_in"] != DBNull.Value && Convert.ToBoolean(reader["pc_in"]);

                            string barcode = reader["barcode_no"]?.ToString() ?? "Bilinmeyen";
                            int id = (int)reader["Id"];

                            // 3. SADECE plc_in 1 ise VE biz henüz işlemi YAKALAMADIYSAK çalıştır
                            if (plc_in && !pc_in_durumu)
                            {
                                _logger.LogInformation($"Yeni kayıt yakalandı! İşlem başlatılıyor. Id: {id}");
                                _ = _handler.HandleAsync(id, barcode);
                            }
                        }
                    }
                }
            }
            DinlemeyeKayitOl();
        }
    }
}
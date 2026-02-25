using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraService.Features;

namespace CameraService.Workers
{
    using Microsoft.Data.SqlClient;

    public class DatabaseTriggerWorker : BackgroundService
    {
        private readonly ILogger<DatabaseTriggerWorker> _logger;
        private readonly SnapshotCaptureHandler _handler;
        private readonly string _connectionString = @"Server=(localdb)\MSSQLLocalDB;Database=TestDB;Integrated Security=true;TrustServerCertificate=True;";

        public DatabaseTriggerWorker(ILogger<DatabaseTriggerWorker> logger, SnapshotCaptureHandler handler)
        {
            _logger = logger;
            _handler = handler;
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
                    
                    using (SqlDataReader reader = cmd.ExecuteReader()) { }
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
                    string sql = "SELECT TOP 1 plc_in, barcode_no FROM dbo.BarcodeTracking ORDER BY Id DESC";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            bool plc_in = reader["plc_in"] != DBNull.Value && (bool)reader["plc_in"];
                            string barcode = reader["barcode_no"]?.ToString() ?? "Bilinmeyen";

                            if (plc_in)
                            {
                                _logger.LogInformation($"plc_in değeri 1! Snapshot başlatılıyor. Barkod: {barcode}");
                                // Handler çağrılır ve işlem delegasyonu yapılır
                                _ = _handler.HandleAsync(barcode);
                            }
                        }
                    }
                }
            }
            DinlemeyeKayitOl();
        }
    }
}

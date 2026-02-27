using Microsoft.EntityFrameworkCore;

namespace CameraService.Features.SnapshotCapture
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Veritabanındaki BarcodeTracking tablosunu temsil eder
        public DbSet<BarcodeRecord> BarcodeTracking { get; set; }
    }
}
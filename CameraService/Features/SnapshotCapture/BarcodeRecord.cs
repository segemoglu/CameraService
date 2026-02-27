using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CameraService.Features.SnapshotCapture
{
    [Table("BarcodeTracking", Schema = "dbo")]
    public class BarcodeRecord
    {
        [Key]
        public int Id { get; set; }
        public string? barcode_no { get; set; }
        public bool? plc_in { get; set; }
        public bool? plc_out { get; set; }
        public bool? pc_in { get; set; }
        public bool? pc_out { get; set; }
    }
}
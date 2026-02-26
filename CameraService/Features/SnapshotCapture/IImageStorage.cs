using System.Threading.Tasks;

namespace CameraService.Features.SnapshotCapture
{
    public interface IImageStorage
    {
        Task SaveImageAsync(byte[] imageBytes, string barcode);
    }
}
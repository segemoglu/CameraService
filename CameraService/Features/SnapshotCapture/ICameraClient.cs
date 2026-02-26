using System.Threading.Tasks;

namespace CameraService.Features.SnapshotCapture
{
    public interface ICameraClient
    {
        Task<byte[]?> GetSnapshotBytesAsync();
    }
}
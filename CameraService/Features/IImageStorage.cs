using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraService.Features
{
    public interface IImageStorage
    {
        Task SaveImageAsync(byte[] imageBytes);
    }
}

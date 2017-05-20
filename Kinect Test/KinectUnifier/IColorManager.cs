using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IColorManager
    {
        event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;
    }

    public class ColorFrameReadyEventArgs
    {
        
    }

    public interface IColorFrame : IDisposable
    {
        int BytesPerPixel { get; }
        int PixelDataLength { get; }

        void CopyFramePixelDataToArray(byte[] buffer);

        int Height { get; }
        int Width { get; }
    }
}

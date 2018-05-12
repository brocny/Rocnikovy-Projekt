using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Core.Kinect
{
    public interface IColorFrameStream
    {
        event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;
        bool IsOpen { get; }
        int FrameWidth { get; }
        int FrameHeight { get; }
        int BytesPerPixel { get; }
        int FrameDataSize { get; }
        IColorFrame GetNextFrame();


        void Open(bool preferResolutionOverFps);
        void Close();
    }

    public class ColorFrameReadyEventArgs : EventArgs
    { 
        public IColorFrame ColorFrame { get; }

        public ColorFrameReadyEventArgs(IColorFrame colorFrame)
        {
            this.ColorFrame = colorFrame;
        }
    }

    public interface IColorFrame : IDisposable
    {
        int BytesPerPixel { get; }
        int PixelDataLength { get; }

        void CopyFramePixelDataToArray(byte[] buffer);
        void CopyFramePixelDataToIntPtr(IntPtr ptr, int pixelDataLength);

        int Height { get; }
        int Width { get; }
    }
}

using System;

namespace Core.Kinect
{
    public interface IColorFrameStream
    {
        event EventHandler<ColorFrameReadyEventArgs> ColorFrameReady;
        bool IsOpen { get; }
        int FrameWidth { get; }
        int FrameHeight { get; }
        int BytesPerPixel { get; }
        /// <summary>
        /// The number of bytes required to store one frame from this stream
        /// </summary>
        int FrameDataSize { get; }
        IColorFrame GetNextFrame();

        /// <summary>
        /// Opens the stream
        /// </summary>
        /// <param name="preferResolutionOverFps">
        /// If set to <c>true</c>, the stream will open with a high resolution/low fps setting, if available.
        /// If there is no such setting, this parameter is ignored
        /// </param>
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
        /// <summary>
        /// The total number of bytes of the image pixels
        /// </summary>
        int PixelDataLength { get; }

        void CopyFramePixelDataToArray(byte[] buffer);
        void CopyFramePixelDataToIntPtr(IntPtr ptr, int pixelDataLength);

        int Height { get; }
        int Width { get; }
    }
}

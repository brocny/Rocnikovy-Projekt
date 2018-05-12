using System;

namespace Core.Kinect
{
    public interface IDepthFrameStream
    {
        void Open();
        void Close();
        int FrameWidth { get; }
        int FrameHeight { get; }
        int MinDistance { get; }
        int MaxDistance { get; }
        bool IsOpen { get; }
        event EventHandler<DepthFrameReadyEventArgs> DepthFrameReady;
        IDepthFrame GetNextFrame();
    }

    public interface IDepthFrame : IDisposable
    {
        int Width { get; }
        int Height { get; }
        void CopyFramePixelDataToArray(ushort[] array);
        void CopyFramePixelDataToIntPtr(IntPtr ptr, int size);
    }


    public class DepthFrameReadyEventArgs : EventArgs
    {
        public IDepthFrame DepthFrame { get; }

        public DepthFrameReadyEventArgs(IDepthFrame depthFrame)
        {
            DepthFrame = depthFrame;
        }
    }
}
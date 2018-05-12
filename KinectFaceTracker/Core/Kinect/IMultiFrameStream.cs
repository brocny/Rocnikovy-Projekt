using System;

namespace Core.Kinect
{
    public interface IMultiFrameStream : IDisposable
    {
        event EventHandler<MultiFrameReadyEventArgs> MultiFrameArrived;
        MultiFrameTypes FrameTypes { get; }
    }

    public class MultiFrameReadyEventArgs
    {
        public MultiFrameReadyEventArgs(IMultiFrame frame)
        {
            MultiFrame = frame;
        }

        public IMultiFrame MultiFrame { get; }
    }

    public interface IMultiFrame : IDisposable
    {
        IColorFrame ColorFrame { get; }
        IBodyFrame BodyFrame { get; }
        IDepthFrame DepthFrame { get; }
    }

    [Flags]
    public enum MultiFrameTypes
    {
        Color = 1 << 0,
        Depth = 1 << 3,
        Body = 1 << 5
    }
}

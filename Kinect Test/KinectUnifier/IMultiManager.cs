using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IMultiManager : IDisposable
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

    public interface IMultiFrame
    {
        IColorFrame ColorFrame { get; }
        IBodyFrame BodyFrame { get; }
    }

    [Flags]
    public enum MultiFrameTypes
    {
        Color = 1 << 0,
        Body = 1 << 5
    }
}

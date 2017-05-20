using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IBodyManager
    {
        event EventHandler<BodyFrameReadyEventArgs> BodyFrameReady;
    }

    public class BodyFrameReadyEventArgs
    {
        
    }

    public interface IBody
    {
        
    }

    public interface ICoord

    

    
}

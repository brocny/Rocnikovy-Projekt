using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface IKinect
    {
        void Open();

        void OpenColorManager();
        void OpenBodyManager();

        IBodyManager BodyManager { get; }
        IColorManager ColorManager { get; }

        bool IsRunning { get; }

        
    }
}

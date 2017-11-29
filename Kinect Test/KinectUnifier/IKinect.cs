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
        bool IsKinectOne { get; }

        void Open();
        void Close();

        IBodyManager BodyManager { get; }
        IColorManager ColorManager { get; }
        ICoordinateMapper CoordinateMapper { get; }
        IMultiManager OpenMultiManager(MultiFrameTypes frameTypes);


        bool IsRunning { get; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;
using KinectOne;
using Kinect360;

namespace KinectFactory
{
    public static class KinectFactory
    {
        public static IKinect GetKinectOne()
        {
            return new KinectOne.KinectOne();
        }

        public static IKinect GetKinect360()
        {
            return new Kinect360.Kinect360();
        }
    }
}

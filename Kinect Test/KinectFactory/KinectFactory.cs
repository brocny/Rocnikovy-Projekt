using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;


namespace KinectFactory
{
    using Kinect360;
    using KinectOne;
    public static class KinectFactory
    {
        public static IKinect GetKinect360()
        {
            return new Kinect360();
        }


        public static IKinect GetKinectOne()
        {
            return new KinectOne();
        }
    }
}

#define ONE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;



namespace KinectFactory
{
#if ONE
    using KinectOne;
#else
    using Kinect360;
#endif

    public static class KinectFactory
    {
        public static IKinect GetKinect360()
        {
#if ONE
            return null;
#else
            return new Kinect360();
#endif
        }


        public static IKinect GetKinectOne()
        {
#if ONE
            return new KinectOne();
#else
            return null;
#endif
        }

        public static IKinect GetKinect()
        {
#if ONE
            return GetKinectOne();
#else
            return GetKinect360();
#endif
        }
    }
}

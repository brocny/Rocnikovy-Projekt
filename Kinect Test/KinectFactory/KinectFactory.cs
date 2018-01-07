using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;

namespace KinectFactory
{

    public static class KinectFactory
    {
        public static IKinect GetKinect360()
        {
#if K_360
            return new Kinect360.Kinect360();
#else
            return null;
#endif
        }


        public static IKinect GetKinectOne()
        {
#if K_ONE
            return new KinectOne.KinectOne();
#else
            return null;
#endif
        }

        public static IKinect GetKinect()
        {
#if K_ONE
            return GetKinectOne();
#elif K_360
            return GetKinect360();
#else
            return null;
#endif
        }
    }
}

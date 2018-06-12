using System;
using Core.Kinect;

namespace KinectFactory
{
#if K_ONE
    using KinectOne;
#elif K_360
    using Kinect360;
#endif

    public static class KinectFactory
    {
        public static IKinect GetKinect360()
        {
#if K_360
            try
            {
                return new Kinect360();
            }
            catch (ApplicationException)
            {
                return null;
            }
#else
            return null;
#endif
        }


        public static IKinect GetKinectOne()
        {
#if K_ONE
            try
            {
                return new KinectOne();
            }
            catch (ApplicationException)
            {
                return null;
            }
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

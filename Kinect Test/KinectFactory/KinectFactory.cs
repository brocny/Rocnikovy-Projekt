using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectUnifier;


namespace KinectFactory
{
    using Kinect360;
    //using KinectOne;

    public static class KinectFactory
    {
        public static IKinect GetKinect360()
        {
            return new Kinect360();
            //return null;

        }


        public static IKinect GetKinectOne()
        {
            //return new KinectOne();
            return null;
        }

        public static IKinect GetKinect()
        {
            return GetKinect360();
            // return GetKinectOne();
        }
    }
}

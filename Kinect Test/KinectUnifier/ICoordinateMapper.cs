using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface ICoordinateMapper
    {
        Point2F MapCameraPointToDepthSpace(Point3F cameraPoint);
        void MapCameraPointsToDepthSpace(Point3F[] cameraPoints, Point2F[] depthPoints);

        Point2F MapCameraPointToColorSpace(Point3F camerPoint);
        void MapCameraPointsToColorSpace(Point3F[] cameraPoints, Point2F colorPoints);


    }
}

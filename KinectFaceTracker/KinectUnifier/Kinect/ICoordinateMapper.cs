using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace KinectUnifier
{
    public interface ICoordinateMapper
    {
        System.Numerics.Vector2 MapCameraPointToDepthSpace(System.Numerics.Vector3 cameraPoint);
        void MapCameraPointsToDepthSpace(System.Numerics.Vector3[] cameraPoints, System.Numerics.Vector2[] depthPoints);

        System.Numerics.Vector2 MapCameraPointToColorSpace(System.Numerics.Vector3 cameraPoint);
        void MapCameraPointsToColorSpace(System.Numerics.Vector3[] cameraPoints, System.Numerics.Vector2[] colorPoints);
    }
}

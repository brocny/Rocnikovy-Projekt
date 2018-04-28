using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public interface ICoordinateMapper
    {
        Vector2 MapCameraPointToDepthSpace(Vector3 cameraPoint);
        void MapCameraPointsToDepthSpace(Vector3[] cameraPoints, Vector2[] depthPoints);

        Vector2 MapCameraPointToColorSpace(Vector3 cameraPoint);
        void MapCameraPointsToColorSpace(Vector3[] cameraPoints, Vector2[] colorPoints);
    }
}

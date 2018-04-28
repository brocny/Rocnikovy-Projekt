using System.Numerics;
using Core;
using Microsoft.Kinect;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;

namespace KinectOne
{
    public class CoordinateMapperOne : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;

        public CoordinateMapperOne(CoordinateMapper coordinateMapper)
        {
            _coordinateMapper = coordinateMapper;

        }

        public void MapCameraPointsToColorSpace(Vector3[] cameraPoints, Vector2[] colorPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < colorPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapCameraPointToColorSpace(new CameraSpacePoint()
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    });

                colorPoints[i] = new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public void MapCameraPointsToDepthSpace(Vector3[] cameraPoints, Vector2[] depthPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < depthPoints.Length; i++)
            {
                var depthSpacePoint =
                _coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint()
                {
                    X = cameraPoints[i].X,
                    Y = cameraPoints[i].Y,
                    Z = cameraPoints[i].Z
                });

                depthPoints[i] = new Vector2(depthSpacePoint.X, depthSpacePoint.Y);
            }
        }

        public Vector2 MapCameraPointToColorSpace(Vector3 cameraPoint)
        {
            var colorSpacePoint =
                _coordinateMapper.MapCameraPointToColorSpace(new CameraSpacePoint
                {
                    X = cameraPoint.X,
                    Y = cameraPoint.Y,
                    Z = cameraPoint.Z
                });
            return new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Vector2 MapCameraPointToDepthSpace(Vector3 cameraPoint)
        {
            var depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint()
            {
                X = cameraPoint.X,
                Y = cameraPoint.Y,
                Z = cameraPoint.Z
            });
            return new Vector2(depthSpacePoint.X, depthSpacePoint.Y);
        }
    }
}

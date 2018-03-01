using KinectUnifier;
using Microsoft.Kinect;

namespace KinectOne
{
    public class CoordinateMapperOne : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;

        public CoordinateMapperOne(CoordinateMapper coordinateMapper)
        {
            _coordinateMapper = coordinateMapper;

        }

        public void MapCameraPointsToColorSpace(Point3F[] cameraPoints, Point2F[] colorPoints)
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

                colorPoints[i] = new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public void MapCameraPointsToDepthSpace(Point3F[] cameraPoints, Point2F[] depthPoints)
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

                depthPoints[i] = new Point2F(depthSpacePoint.X, depthSpacePoint.Y);
            }
        }

        public Point2F MapCameraPointToColorSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
                _coordinateMapper.MapCameraPointToColorSpace(new CameraSpacePoint()
                {
                    X = cameraPoint.X,
                    Y = cameraPoint.Y,
                    Z = cameraPoint.Z
                });
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Point2F MapCameraPointToDepthSpace(Point3F cameraPoint)
        {
            var depthSpacePoint = _coordinateMapper.MapCameraPointToDepthSpace(new CameraSpacePoint()
            {
                X = cameraPoint.X,
                Y = cameraPoint.Y,
                Z = cameraPoint.Z
            });
            return new Point2F(depthSpacePoint.X, depthSpacePoint.Y);
        }
    }
}

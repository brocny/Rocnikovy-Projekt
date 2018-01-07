using KinectUnifier;
using Microsoft.Kinect;


namespace Kinect360
{
    internal class CoordinateMapper360 : ICoordinateMapper
    {
        private CoordinateMapper _coordinateMapper;
        private Kinect360 _kinect360;

        public CoordinateMapper360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _coordinateMapper = _kinect360.KinectSensor.CoordinateMapper;
        }

        public void MapCameraPointsToColorSpace(Point3F[] cameraPoints, Point2F[] colorPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < colorPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    }, _kinect360.ColorManager == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorManager.ColorImageFormat);

                colorPoints[i] = new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public void MapCameraPointsToDepthSpace(Point3F[] cameraPoints, Point2F[] depthPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < depthPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    }, DepthImageFormat.Resolution640x480Fps30);

                depthPoints[i] = new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public Point2F MapCameraPointToColorSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, _kinect360.ColorManager == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorManager.ColorImageFormat);
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Point2F MapCameraPointToDepthSpace(Point3F cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, DepthImageFormat.Resolution640x480Fps30);
            return new Point2F(colorSpacePoint.X, colorSpacePoint.Y);
        }
    }


}
using System.Numerics;
using Core.Kinect;
using Microsoft.Kinect;


namespace Kinect360
{
    public class CoordinateMapper360 : ICoordinateMapper
    {
        private readonly CoordinateMapper _coordinateMapper;
        private readonly Kinect360 _kinect360;

        public CoordinateMapper360(Kinect360 kinect360)
        {
            _kinect360 = kinect360;
            _coordinateMapper = _kinect360.KinectSensor.CoordinateMapper;
            
        }

        public void MapCameraPointsToColorSpace(Vector3[] cameraPoints, Vector2[] colorPoints)
        {
            for (int i = 0; i < cameraPoints.Length && i < colorPoints.Length; i++)
            {
                var colorSpacePoint =
                    _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint
                    {
                        X = cameraPoints[i].X,
                        Y = cameraPoints[i].Y,
                        Z = cameraPoints[i].Z
                    }, _kinect360.ColorFrameStream == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorFrameStream.ColorImageFormat);

                colorPoints[i] = new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public void MapCameraPointsToDepthSpace(Vector3[] cameraPoints, Vector2[] depthPoints)
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

                depthPoints[i] = new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
            }
        }

        public Vector2 MapCameraPointToColorSpace(Vector3 cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToColorPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, _kinect360.ColorFrameStream == null ? ColorImageFormat.RgbResolution640x480Fps30 : _kinect360._colorFrameStream.ColorImageFormat);
            return new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
        }

        public Vector2 MapCameraPointToDepthSpace(Vector3 cameraPoint)
        {
            var colorSpacePoint =
               _coordinateMapper.MapSkeletonPointToDepthPoint(new SkeletonPoint()
               {
                   X = cameraPoint.X,
                   Y = cameraPoint.Y,
                   Z = cameraPoint.Z
               }, DepthImageFormat.Resolution640x480Fps30);
            return new Vector2(colorSpacePoint.X, colorSpacePoint.Y);
        }
    }


}
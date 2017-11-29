using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFace2
    {
        private int[] _faceImageHandles;
        private FSDK.TFacePosition[] _facePositions;
        private FSDK.TPoint[][] _facialFeatures;
        private Rectangle[] _faceRectangles;
        private Size[] _translations;
        static object fihLock = new object();

        public int DetectionThreshold
        {
            set { FSDK.SetFaceDetectionThreshold(value); }
        }

        public LuxandFace2()
        {
            FSDK.SetFaceDetectionParameters(false, false, 75);
        }

        public LuxandFace2(int frameWidth, int frameHeight, int frameBytesPerPixel)
        {
            FrameHeight = frameHeight;
            FrameWidth = frameWidth;
            FrameBytesPerPixel = frameBytesPerPixel;
        }

        public LuxandFace2(int frameWidth, int frameHeight, int frameBytesPerPixel, int detectionThreshould) : this(frameWidth, frameHeight, frameBytesPerPixel)
        {
            DetectionThreshold = detectionThreshould;
        }

        public void InitializeLibrary()
        {
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(
                @"qkCo6wATHvarVxeIrN1PI/b1aBxCe1GYdqhGmWEQ3VQqEmjgBtGUDBn5oyu9DqUSxsI4YABRzKDYQ/7Y0MCARdMJs7bgxBt7npmXidPq/4qPgC6bzQZ/bzk9VJBtMBQ08c8T6855C5NDnw8L3QybU+Ou0tnmMN3CtM8mhjQCtvQ="))
            {
                throw new ApplicationException("Invalid Luxand FSDK Key!");
            }

            FSDK.InitializeLibrary();
        }


        public Image GetFaceImage(int faceIndex)
        {
            if (_faceImageHandles == null) return null;
            return new FSDK.CImage(_faceImageHandles[faceIndex]).ToCLRImage();
        }

        public Point[] GetFacialFeatures(int faceIndex)
        {
            if (_facePositions?[faceIndex] == null || _translations?[faceIndex] == null) return null;
            FSDK.TPoint[] temp;
            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialFeaturesInRegion(_faceImageHandles[faceIndex], ref _facePositions[faceIndex],
                    out temp))
            {
                return null;
            }
            if(_facialFeatures == null) _facialFeatures = new FSDK.TPoint[FaceCount][];
            _facialFeatures[faceIndex] = temp;
            return temp?.Select(x => x.ToPoint() + _translations[faceIndex]).ToArray();
        }

        public byte[] GetFaceTemplate(int faceIndex)
        {
            if (_facialFeatures?[faceIndex] != null)
            {
                if(FSDK.FSDKE_OK == FSDK.GetFaceTemplateUsingFeatures(_faceImageHandles[faceIndex], ref _facialFeatures[faceIndex], out var ret))
                {
                    return ret;
                }
                return null;
            }
            
            var facePostion = _facePositions?[faceIndex];
            if (facePostion == null) return null;
            byte[] retVal;
            if (FSDK.FSDKE_OK != FSDK.GetFaceTemplateInRegion(_faceImageHandles[faceIndex], ref facePostion, out retVal))
            {
                return null;
            }

            return retVal;
        }

        public void FeedFaces(byte[][] buffers, int[] widths, int[] heights, int bytesPerPixel = 4)
        {
            if (_faceImageHandles != null)
            {
                foreach (var handle in _faceImageHandles)
                {
                    FSDK.FreeImage(handle);
                }
            }

            _facialFeatures = null;
            _faceImageHandles = new int[buffers.Length];
            _facePositions = new FSDK.TFacePosition[buffers.Length];
            FSDK.FSDK_IMAGEMODE imageMode = LuxandUtil.ImageModeFromBytesPerPixel(bytesPerPixel);
            for (var i = 0; i < buffers.Length; i++)
            {
                FSDK.LoadImageFromBuffer(ref _faceImageHandles[i],
                    buffers[i],
                    widths[i],
                    heights[i],
                    widths[i] * bytesPerPixel,
                    imageMode);
            }

            DetectFaces();
        }

        public void FeedFaces(byte[] buffer, Rectangle[] faceRectangles, int bytesPerPixel)
        { 
            if (_faceImageHandles != null)
            {
                lock (fihLock)
                    foreach (var handle in _faceImageHandles)
                    {
                    FSDK.FreeImage(handle);
                    }
            }

            _faceRectangles = faceRectangles;
            _translations = new Size[faceRectangles.Length];
            _faceImageHandles = new int[faceRectangles.Length];
            _facePositions = new FSDK.TFacePosition[faceRectangles.Length];
            Thread.MemoryBarrier();
            var imageMode = LuxandUtil.ImageModeFromBytesPerPixel(bytesPerPixel);

            for (int i = 0; i < faceRectangles.Length; i++)
            {
                var fp = faceRectangles[i].TrimRectangle(FrameWidth, FrameHeight);
                _translations[i] = new Size(fp.Location);
                var locBuffer = buffer.GetBufferRect(FrameWidth, fp, bytesPerPixel);
                lock(fihLock)
                FSDK.LoadImageFromBuffer(ref _faceImageHandles[i],
                    locBuffer, 
                    fp.Width,
                    fp.Height, 
                    fp.Width * bytesPerPixel,
                    imageMode);
            }

            DetectFaces();
        }

        public void DetectFaces()
        {
            Parallel.For(0, _faceRectangles.Length, i =>
            { 
                FSDK.TFacePosition detectedFace = new FSDK.TFacePosition();
                lock (fihLock)
                    if (FSDK.FSDKE_OK != FSDK.DetectFace(_faceImageHandles[i], ref detectedFace))
                    {
                    //nothing
                    }                   
                   
                
                _facePositions[i] = detectedFace;
            });

        }
        
        public void FeedFacePositions(Rectangle[] facePositions)
        {
            _facePositions = facePositions.Select(r => LuxandUtil.RectRotAngleToTFacePosition(r, 0)).ToArray();
        }

        public void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngles)
        {
            _facePositions = facePositions.Select((r, a) => LuxandUtil.RectRotAngleToTFacePosition(r, a)).ToArray();
        }

        public Rectangle[] GetFacePositions()
        {
            return _facePositions?.Select(x => LuxandUtil.TFacePositionToRectRotAngle(x).Item1).ToArray();
        }

        public (Rectangle rect, double rotAngle)[] GetFacePostionsAndRotationAngles()
        {
            return _facePositions?.Select(LuxandUtil.TFacePositionToRectRotAngle).ToArray();
        }

        public int FaceCount => _faceImageHandles?.Length ?? 0;
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int FrameBytesPerPixel { get; set; }
        public Rectangle[] FaceRectangles => _faceRectangles;

        public static int FaceFeatureCount => FSDK.FSDK_FACIAL_FEATURE_COUNT;
    }
}

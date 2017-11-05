using System;
using System.Drawing;
using System.Linq;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFace2 : IFaceLIb
    {
        private IKinect _kinect;
        private IBody[] _bodies;
        private int _lastImageHandle;
        private int[] _lastFaceImageHandles;
        private FSDK.CImage[] _lastFaceImages;
        private FSDK.TFacePosition[] _facePositions;
        private Rectangle[] _faceRects;

        private byte[] _frameBuffer;

        public LuxandFace2()
        {
            FSDK.SetFaceDetectionParameters(false, true, 70);
            FSDK.SetFaceDetectionThreshold(3);
        }

        public LuxandFace2(int frameWidth, int frameHeight, int frameBytesPerPixel)
        {
            FrameHeight = frameHeight;
            FrameWidth = frameWidth;
            FrameBytesPerPixel = frameBytesPerPixel;
        }

        public void InitializeLibrary()
        {
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(
                @"i7+h0CMkmL01bh7u5pwc55VtkWdGfAP8xr9YV+mSKMCLrjzHCS7Izg1gaD2OM9kJlAJj3gaXwdrDwKRP8RJNhKxO/HsmoBb+LYwUXfUmSg+h9zTrgKCY/85w89YxkR4x9uOYgxK8ah9am2ZaVJtPgEs+I1GZAXmFjSBbtUkelZU="))
            {
                throw new ApplicationException("Invalid Luxand FSDK Key!");
            }

            FSDK.InitializeLibrary();
        }


        public Image GetFaceImage(int faceIndex)
        {
            if (_lastFaceImageHandles == null) return null;
            return new FSDK.CImage(_lastFaceImageHandles[faceIndex]).ToCLRImage();
        }

        public Point[] GetFacialFeatures(int faceIndex)
        {
            var facePosition = _facePositions?[faceIndex];
            if (facePosition == null) return null;
            FSDK.TPoint[] temp;

            if (FSDK.FSDKE_OK != FSDK.DetectFacialFeaturesInRegion(_lastImageHandle, ref facePosition, out temp))
                return null;

            return temp.Select(x => x.ToPoint()).ToArray();
        }

        public byte[] GetFaceTemplate(int faceIndex)
        {
            //if (_facePositions == null) return null;
            var facePostion = _facePositions[faceIndex];
            if (facePostion == null) return null;
            if (FSDK.FSDKE_OK != FSDK.GetFaceTemplateInRegion(_lastImageHandle, ref facePostion, out var retVal))
            {
                return null;
            }

            return retVal;
        }

        public Image GetImage(ref IntPtr bmpIntPtr)
        {
            return new FSDK.CImage(_lastImageHandle).ToCLRImage();
        }

        public void FeedFrame(byte[] buffer)
        {
            if (buffer == null)
            {
                return;
            }
            _frameBuffer = buffer;
            FSDK.FreeImage(_lastImageHandle);

            FSDK.LoadImageFromBuffer(ref _lastImageHandle, _frameBuffer, FrameWidth, FrameHeight, FrameWidth * FrameBytesPerPixel,
                FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_32BIT);
        }

        public void FeedFrame(Bitmap bmp)
        {
            var hBmp = bmp.GetHbitmap();
            FSDK.LoadImageFromHBitmap(ref _lastImageHandle, hBmp);
        }

        public void GenerateFaceImages()
        {
            if (_faceRects == null || _faceRects.Length == 0) return;
            if (_lastFaceImageHandles != null)
            {
                foreach (var handle in _lastFaceImageHandles)
                {
                    FSDK.FreeImage(handle);
                }
            }

            _lastFaceImageHandles = new int[_faceRects.Length];
            for (int i = 0; i < _faceRects.Length; i++)
            {
                FSDK.CreateEmptyImage(ref _lastFaceImageHandles[i]);
                var r = _faceRects[i];
                FSDK.CopyRect(_lastImageHandle, r.Left, r.Top, r.Right, r.Bottom, _lastFaceImageHandles[i]);
            }
            for (int i = 0; i < _lastFaceImageHandles.Length; i++)
            {
                FSDK.DetectFace(_lastFaceImageHandles[i], ref _facePositions[i]);
                if (_facePositions[i] == null) return;
                _facePositions[i].xc += _faceRects[i].X;
                _facePositions[i].yc += _faceRects[i].Y;
            }
        }

        public void FeedFacePositions(Rectangle[] facePositions)
        {
            //_facePositions = facePositions.Select(x => LuxandUtil.RectRotAngleToTFacePosition(x, 0)).ToArray();
            _faceRects = new Rectangle[facePositions.Length];
            facePositions.CopyTo(_faceRects, 0);
            if (_faceRects.Length == 0) return;
            _facePositions = new FSDK.TFacePosition[facePositions.Length];
            GenerateFaceImages();
        }

        public void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngles)
        {
            //_facePositions = facePositions.Select((r, a) => LuxandUtil.RectRotAngleToTFacePosition(r, a)).ToArray();
        }

        public Rectangle[] GetFacePositions()
        {
            int faceCount = 0;
            FSDK.DetectMultipleFaces(_lastImageHandle, ref faceCount, out var fPositions, sizeof(long) * 16);
            return fPositions.Select(x => LuxandUtil.TFacePositionToRectRotAngle(x).Item1).ToArray();
        }

        public (Rectangle rect, double rotAngle)[] GetFacePostionsAndRotationAngles()
        {
            int faceCount = 0;
            FSDK.DetectMultipleFaces(_lastImageHandle, ref faceCount, out var fPositions,
                sizeof(long) * 16);
            return fPositions.Select(LuxandUtil.TFacePositionToRectRotAngle).ToArray();
        }

        public int NumberOfFaces { get; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int FrameBytesPerPixel { get; set; }
    }
}

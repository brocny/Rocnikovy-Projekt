using System;
using System.Drawing;
using System.Linq;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFace2
    {
        private int[] _faceImageHandles;
        private FSDK.TFacePosition[] _facePositions;
        private FSDK.TPoint[][] _facialFeatures;

        public int DetectionThreshold
        {
            set { FSDK.SetFaceDetectionThreshold(value); }
        }

        public LuxandFace2()
        {

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
                @"i7+h0CMkmL01bh7u5pwc55VtkWdGfAP8xr9YV+mSKMCLrjzHCS7Izg1gaD2OM9kJlAJj3gaXwdrDwKRP8RJNhKxO/HsmoBb+LYwUXfUmSg+h9zTrgKCY/85w89YxkR4x9uOYgxK8ah9am2ZaVJtPgEs+I1GZAXmFjSBbtUkelZU="))
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
            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialFeaturesInRegion(_faceImageHandles[faceIndex], ref _facePositions[faceIndex],
                    out var temp))
            {
                return null;
            }

            _facialFeatures[faceIndex] = temp;
            return temp?.Select(x => x.ToPoint()).ToArray();
        }

        public byte[] GetFaceTemplate(int faceIndex)
        {
            if (_facialFeatures[faceIndex] != null)
            {
                if(FSDK.FSDKE_OK == FSDK.GetFaceTemplateUsingFeatures(_faceImageHandles[faceIndex], ref _facialFeatures[faceIndex], out var ret))
                {
                    return ret;
                }
                return null;
            }
            
            var facePostion = _facePositions?[faceIndex];
            if (facePostion == null) return null;
            if (FSDK.FSDKE_OK != FSDK.GetFaceTemplateInRegion(_faceImageHandles[faceIndex], ref facePostion, out var retVal))
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
            FSDK.FSDK_IMAGEMODE imageMode = LuxandUtil.ImageModeFromBytersPerPixel(bytesPerPixel);
            for (var i = 0; i < buffers.Length; i++)
            {
                FSDK.LoadImageFromBuffer(ref _faceImageHandles[i],
                    buffers[i],
                    widths[i],
                    heights[i],
                    widths[i] * bytesPerPixel,
                    imageMode);
                FSDK.DetectFace(_faceImageHandles[i], ref _facePositions[i]);
            }
        }

        public void FeedFaces(byte[] buffer, Rectangle[] facePositions, int bytesPerPixel)
        {
            if (_faceImageHandles != null)
            {
                foreach (var handle in _faceImageHandles)
                {
                    FSDK.FreeImage(handle);
                }
            }

            _faceImageHandles = new int[facePositions.Length];
            var imageMode = LuxandUtil.ImageModeFromBytersPerPixel(bytesPerPixel);

            for (int i = 0; i < facePositions.Length; i++)
            {
                var locBuffer = buffer.GetBufferRect(facePositions[i], bytesPerPixel);
                FSDK.LoadImageFromBuffer(ref _faceImageHandles[i],
                    locBuffer, 
                    facePositions[i].Width,
                    facePositions[i].Height, 
                    facePositions[i].Width * bytesPerPixel,
                    imageMode);
            }
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

        public int FaceCount { get; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int FrameBytesPerPixel { get; set; }

        public static int FaceFeatureCount => FSDK.FSDK_FACIAL_FEATURE_COUNT;
    }
}

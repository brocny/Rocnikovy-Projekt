﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFace : IFaceLIb
    {
        private int _lastImageHandle;
        private int[] _lastFaceImageHandles;
        private FSDK.TFacePosition[] _facePositions;
        private FSDK.FSDK_IMAGEMODE _imageMode;

        private byte[] _frameBuffer;
        
        public LuxandFace()
        {
            
        }

        public LuxandFace(int frameWidth, int frameHeight, int frameBytesPerPixel)
        {
            FrameHeight = frameHeight;
            FrameWidth = frameWidth;
            FrameBytesPerPixel = frameBytesPerPixel;
            _imageMode = LuxandUtil.ImageModeFromBytersPerPixel(frameBytesPerPixel);
        }

        public void InitializeLibrary()
        {
            if(FSDK.FSDKE_OK != FSDK.ActivateLibrary(
                @"i7+h0CMkmL01bh7u5pwc55VtkWdGfAP8xr9YV+mSKMCLrjzHCS7Izg1gaD2OM9kJlAJj3gaXwdrDwKRP8RJNhKxO/HsmoBb+LYwUXfUmSg+h9zTrgKCY/85w89YxkR4x9uOYgxK8ah9am2ZaVJtPgEs+I1GZAXmFjSBbtUkelZU="))
            {
                throw new ApplicationException("Invalid Luxand FSDK Key!");    
            }

            FSDK.InitializeLibrary();
        }


        public Image GetFaceImage(int faceIndex)
        {
            return new FSDK.CImage(_lastFaceImageHandles[faceIndex]).ToCLRImage();
        }

        public Point[] GetFacialFeatures(int faceIndex)
        {
            var facePosition = _facePositions[faceIndex];
            FSDK.TPoint[] temp;

            if (FSDK.FSDKE_OK != FSDK.DetectFacialFeaturesInRegion(_lastImageHandle, ref facePosition, out temp))
                return null;

            return temp.Select(x => x.ToPoint()).ToArray();
        }

        public byte[] GetFaceTemplate(int faceIndex)
        {
           
            var facePostion = _facePositions[faceIndex];
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
            for (int i = 0; i < _facePositions.Length; i++)
            {
                FSDK.CreateEmptyImage(ref _lastFaceImageHandles[i]);
                var x = _facePositions[i].xc - _facePositions[i].w;
                var y = _facePositions[i].yc - _facePositions[i].w;
                FSDK.CopyRect(_lastImageHandle, x, y, 0, 0, _lastFaceImageHandles[i]);
            }
        }

        public void FeedFacePositions(Rectangle[] facePositions)
        {
            _facePositions = facePositions.Select(x => LuxandUtil.RectRotAngleToTFacePosition(x, 0)).ToArray();

        }

        public void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngles)
        {
            _facePositions = facePositions.Select((r, a) => LuxandUtil.RectRotAngleToTFacePosition(r, a)).ToArray();
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

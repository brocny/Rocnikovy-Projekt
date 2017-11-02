﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class LuxandFace : IFaceLIb
    {
        private IKinect _kinect;
        private IBody[] _bodies;
        private int _lastImageHandle;
        private FSDK.CImage[] _lastFaceImages;
        private FSDK.TFacePosition[] _facePositions;

        private byte[] _frameBuffer;
        
        public LuxandFace()
        {
            
        }

        public LuxandFace(int frameWidth, int frameHeight, int frameBytesPerPixel)
        {
            FrameHeight = frameHeight;
            FrameWidth = frameWidth;
            FrameBytesPerPixel = frameBytesPerPixel;
            if(frameBytesPerPixel != 4)
                throw new Exception();
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

        public void FeedFacePositions(Rectangle[] facePositions)
        {
            _facePositions = facePositions.Select(x => new FSDK.TFacePosition(){angle = 0, w = x.Width, xc = x.X, yc = x.Y}).ToArray();
        }

        public void FeedFacePositions(Rectangle[] facePositions, double[] rotationAngle)
        {
            _facePositions = new FSDK.TFacePosition[facePositions.Length];
            for (var i = 0; i < facePositions.Length; i++)
            {
                var f = facePositions[i];
                _facePositions[i] = new FSDK.TFacePosition(){angle = rotationAngle[i], w = f.Width, xc = f.X + f.Width / 2, yc = f.Y + f.Height / 2};
            }
        }

        public Rectangle[] GetFacePostions()
        {
            throw new NotImplementedException();
        }

        public int NumberOfFaces { get; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public int FrameBytesPerPixel { get; set; }
    }
    public static class TPointExtensions
    {
        public static Point ToPoint(this FSDK.TPoint point)
        {
            return new Point(point.x, point.y);
        }
    }
}

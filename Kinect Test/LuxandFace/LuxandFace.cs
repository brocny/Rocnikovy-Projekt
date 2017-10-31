using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFace
{
    public class LuxandFace : IFaceLIb
    {
        private IKinect _kinect;
        private IBody[] _bodies;
        private FSDK.CImage _lastImage = new FSDK.CImage();
        private FSDK.CImage[] _lastFaceImage;
        private List<FSDK.TFacePosition> _facePositions;

        private byte[] _frameBuffer;
        

        public void InitializeLibrary()
        {
            if(FSDK.FSDKE_OK != FSDK.ActivateLibrary(
                @"i7+h0CMkmL01bh7u5pwc55VtkWdGfAP8xr9YV+mSKMCLrjzHCS7Izg1gaD2OM9kJlAJj3gaXwdrDwKRP8RJNhKxO/HsmoBb+LYwUXfUmSg+h9zTrgKCY/85w89YxkR4x9uOYgxK8ah9am2ZaVJtPgEs+I1GZAXmFjSBbtUkelZU="))
            {
                throw new ApplicationException("Invalid Luxand FSDK Key!");    
            }
            
            FSDK.InitializeLibrary();

        }

        public void BindKinect(IKinect kinect)
        {
            _kinect = kinect;
            _kinect.Open();
            _kinect.ColorManager.ColorFrameReady += ColorManagerOnColorFrameReady;
            _kinect.BodyManager.BodyFrameReady += BodyManagerOnBodyFrameReady;
        }

        private void BodyManagerOnBodyFrameReady(object sender, BodyFrameReadyEventArgs e)
        {
            using (var frame = e.BodyFrame)
            {
                if (frame == null) return;

                if (_bodies == null || _bodies.Length < frame.BodyCount)
                {
                    _bodies = new IBody[frame.BodyCount];
                }

                frame.CopyBodiesTo(_bodies);
                _facePositions.Clear();
                foreach (var body in _bodies)
                {
                    if(!Util.TryGetHeadRectangleInColorSpace(body, _kinect.CoordinateMapper, out var rect, out var rotAngle))
                    {
                        continue;
                    }

                    _facePositions.Add(new FSDK.TFacePosition()
                    {
                        angle = rotAngle,
                        w = rect.Width,
                        xc = rect.X - rect.Width,
                        yc = rect.Y - rect.Height
                    });
                }

            }
        }

        private void ColorManagerOnColorFrameReady(object sender, ColorFrameReadyEventArgs e)
        {
            int frameWidth;
            int frameHeight;
            int frameBpp;
            using (var frame = e.ColorFrame)
            {
                if (frame == null) return;
                if (_frameBuffer == null || _frameBuffer.Length < frame.PixelDataLength)
                {
                    _frameBuffer = new byte[frame.PixelDataLength];
                }
                frameWidth = frame.Width;
                frameHeight = frame.Height;
                frameBpp = frame.BytesPerPixel;
                e.ColorFrame.CopyFramePixelDataToArray(_frameBuffer);
            }

            int lastImageHandle = _lastImage.ImageHandle;
            FSDK.LoadImageFromBuffer(ref lastImageHandle, _frameBuffer, frameWidth, frameHeight, frameWidth * frameBpp,
                FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_32BIT);
        }
    }
}

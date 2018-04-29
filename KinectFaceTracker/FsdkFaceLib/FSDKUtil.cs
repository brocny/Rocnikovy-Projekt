using System;
using System.Diagnostics;
using System.Drawing;
using Core;
using Luxand;

namespace FsdkFaceLib
{
    public static class FsdkUtil
    {
        public static (Rectangle rect, double rotAngle) TFacePositionToRectRotAngle(FSDK.TFacePosition tfp)
        {
            return (new Rectangle(tfp.xc - tfp.w, tfp.yc - tfp.w, tfp.w, tfp.w), tfp.angle);
        }

        public static FSDK.TFacePosition RectRotAngleToTFacePosition(Rectangle rect, double rotAngle)
        {
            return new FSDK.TFacePosition
            {
                angle = rotAngle,
                w = rect.Width,
                xc = rect.X + rect.Width / 2,
                yc = rect.Y + rect.Width / 2
            };
        }

        public static ImageBuffer ImageHandleToImmutableImage(int imageHandle, FSDK.FSDK_IMAGEMODE imageMode = FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_32BIT)
        {
            FSDK.SaveImageToBuffer(imageHandle, out var buffer, imageMode);

            int width = 0, height = 0;
            FSDK.GetImageWidth(imageHandle, ref width);
            FSDK.GetImageHeight(imageHandle, ref height);

            return new ImageBuffer(buffer, width, height, imageMode.BytesPerPixel());
        }

        public static FSDK.FSDK_IMAGEMODE ImageModeFromBytesPerPixel(int bytesPerPixel)
        {
            switch (bytesPerPixel)
            {
                case 4:
                    return FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_32BIT;
                case 3:
                    return FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_24BIT;
                case 1:
                    return FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_GRAYSCALE_8BIT;
                default:
                    throw new ArgumentOutOfRangeException($"{nameof(bytesPerPixel)} invalid value {bytesPerPixel}: supported values are: 1; 3; 4");
            }
        }

        public static int BytesPerPixel(this FSDK.FSDK_IMAGEMODE imageMode)
        {
            switch (imageMode)
            {
                case FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_24BIT:
                    return 3;
                case FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_COLOR_32BIT:
                    return 4;
                case FSDK.FSDK_IMAGEMODE.FSDK_IMAGE_GRAYSCALE_8BIT:
                    return 0;
                default:
                    throw new ArgumentException(nameof(imageMode));
            }
        }
    }

    public static class TPointExtensions
    {
        public static Point ToPoint(this FSDK.TPoint point)
        {
            return new Point(point.x, point.y);
        }
    }


    public static class ImageBufferExtensions
    {
        /// <summary>
        /// Make and return FSDK FaceImage from a <see cref="ImageBuffer"/>
        /// </summary>
        /// <param name="imageBuffer"></param>
        /// <param name="imageHandle"></param>
        /// <returns>Handle to newly created FSDK FaceImage</returns>
        public static int CreateFsdkImageHandle(this ImageBuffer imageBuffer, out int imageHandle)
        {
            imageHandle = -1;
            var buffer = new byte[imageBuffer.Buffer.Length];
            int bpp = imageBuffer.BytesPerPixel;
            int bufferLength = imageBuffer.Buffer.Length;
            unsafe
            {
                fixed (byte* ob = imageBuffer.Buffer)
                fixed (byte* p = buffer)
                {
                    for (int i = 0; i < bufferLength; i += bpp)
                    {
                        p[i] = ob[i + 2];
                        p[i + 1] = ob[i + 1];
                        p[i + 2] = ob[i];
                        p[i + 3] = ob[i + 3];
                    }
                }
            }

            int ret =  FSDK.LoadImageFromBuffer(ref imageHandle, buffer, imageBuffer.Width, imageBuffer.Height, imageBuffer.Width * imageBuffer.BytesPerPixel,
                FsdkUtil.ImageModeFromBytesPerPixel(imageBuffer.BytesPerPixel));

            return ret;
        }
    }
}

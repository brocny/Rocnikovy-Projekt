﻿using System;
using System.Drawing;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    internal static class LuxandUtil
    {
        public static (Rectangle rect, double rotAngle) TFacePositionToRectRotAngle(FSDK.TFacePosition tfp)
        {
            return (new Rectangle(tfp.xc - tfp.w, tfp.yc - tfp.w, tfp.w, tfp.w), tfp.angle);
        }

        internal static FSDK.TFacePosition RectRotAngleToTFacePosition(Rectangle rect, double rotAngle)
        {
            return new FSDK.TFacePosition()
            {
                angle = rotAngle,
                w = rect.Width,
                xc = rect.X + rect.Width / 2,
                yc = rect.Y + rect.Width / 2
            };
        }

        internal static FSDK.FSDK_IMAGEMODE ImageModeFromBytesPerPixel(int bytesPerPixel)
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
    }

    public static class TPointExtensions
    {
        public static Point ToPoint(this FSDK.TPoint point)
        {
            return new Point(point.x, point.y);
        }
    }


    public static class BufferImageExtensions
    {
        /// <summary>
        /// Make and FSDK FaceImage from a <see cref="ImmutableImage"/>
        /// </summary>
        /// <param name="immutableImage"></param>
        /// <param name="imageHandle"></param>
        /// <returns>Handle to newly created FSDK FaceImage</returns>
        public static int ToFsdkImage(this ImmutableImage immutableImage, out int imageHandle)
        {
            imageHandle = -1;
            var buffer = immutableImage.Buffer;
            int ret =  FSDK.LoadImageFromBuffer(ref imageHandle, buffer, immutableImage.Width, immutableImage.Height, immutableImage.Width * immutableImage.BytesPerPixel,
                LuxandUtil.ImageModeFromBytesPerPixel(immutableImage.BytesPerPixel));

            return ret;
        }
    }
}

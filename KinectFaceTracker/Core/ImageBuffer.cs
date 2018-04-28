using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Core
{
    public class ImageBuffer
    {
        public ImageBuffer()
        {
        }

        public ImageBuffer(byte[] buffer, int width, int height, int bytesPerPixel)
        {
            Width = width;
            Height = height;
            BytesPerPixel = bytesPerPixel;
            Buffer = buffer;
        }

        public ImageBuffer(Bitmap bmp)
        {
            int bufLength = bmp.Height * bmp.Width * 4;
            Buffer = new byte[bufLength];
            BytesPerPixel = 4;
            Width = bmp.Width;
            Height = bmp.Height;
            var bits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bits.Scan0, Buffer, 0, bufLength);
        }

        /// <summary>
        /// Returns a copy of the pixel buffer
        /// </summary>
        public byte[] Buffer { get; }

        public int Width { get; }
        public int Height { get; }
        public int BytesPerPixel { get; }

        public ImageBuffer GetRectangle(Rectangle rect)
        {
            return new ImageBuffer(Buffer.GetBufferRect(Width, rect, BytesPerPixel), rect.Width, rect.Height, BytesPerPixel);
        }

        public Bitmap ToBitmap()
        {
            return Buffer.BytesToBitmap(Width, Height, BytesPerPixel);
        }
    }
}
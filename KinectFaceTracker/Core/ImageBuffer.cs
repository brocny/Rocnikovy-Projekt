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
            int bytesPerPixel = bmp.PixelFormat.BytesPerPixel();
            int bufferLength = bmp.Height * bmp.Width * bytesPerPixel;
            Buffer = new byte[bufferLength];
            BytesPerPixel = bytesPerPixel;
            Width = bmp.Width;
            Height = bmp.Height;
            var bits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                bmp.PixelFormat);
            Marshal.Copy(bits.Scan0, Buffer, 0, bufferLength);
        }

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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace KinectUnifier
{
    public class ImmutableImage
    {
        public ImmutableImage()
        {
            
        }

        public ImmutableImage(byte[] buffer, int width, int height, int bytesPerPixel)
        {
            Width = width;
            Height = height;
            BytesPerPixel = bytesPerPixel;
            _buffer = buffer;
        }

        public ImmutableImage(Bitmap bmp)
        {
            int bufLength = bmp.Height * bmp.Width * 4;
            _buffer = new byte[bufLength];
            BytesPerPixel = 4;
            Width = bmp.Width;
            Height = bmp.Height;
            var bits = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(bits.Scan0, _buffer, 0, bufLength);
            
        }

        private readonly byte[] _buffer;

        public byte[] Buffer => (byte[]) _buffer.Clone();

        public int Width { get; }
        public int Height { get; }
        public int BytesPerPixel { get; }

        public Bitmap ToBitmap()
        {
            return _buffer.BytesToBitmap(Width, Height, BytesPerPixel);
        }
    }
}

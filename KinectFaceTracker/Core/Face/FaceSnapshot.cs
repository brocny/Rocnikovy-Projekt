using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using Core;

namespace Core.Face
{
    [Serializable]
    public abstract class FaceSnapshot<T>
    {
        protected const string NoImage = "NO_IMAGE";

        protected FaceSnapshot()
        {
        }

        protected FaceSnapshot(T template, ImageBuffer imageBuffer)
        {
            FaceImageBuffer = imageBuffer;
            Template = template;
        }

        [XmlIgnore]
        public T Template { get; protected set; }

        [XmlIgnore]
        public ImageBuffer FaceImageBuffer { get; protected set; }

        [Browsable(false)]
        [XmlElement("Image")]
        public string XmlImage
        {
            get
            {
                if (FaceImageBuffer == null) return NoImage;
                using (var ms = new MemoryStream())
                {
                    FaceImageBuffer.ToBitmap().Save(ms, ImageFormat.Png);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }

            set
            {
                if (value == NoImage)
                {
                    FaceImageBuffer = null;
                    return;
                }

                var bytes = Convert.FromBase64String(value);
                using (var ms = new MemoryStream(bytes))
                {
                    var image = new Bitmap(ms);
                    FaceImageBuffer = new ImageBuffer(image);
                }
            }
        }

        [Browsable(false)]
        [XmlIgnore]
        public abstract string XmlTemplate { get; set; }
    }
}
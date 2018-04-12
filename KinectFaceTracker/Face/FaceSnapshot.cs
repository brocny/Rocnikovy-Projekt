using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;
using KinectUnifier;

namespace Face
{
    [Serializable]
    public abstract class FaceSnapshot<T>
    {
        [XmlIgnore]
        public T Template { get; protected set; }

        [XmlIgnore]
        public ImmutableImage FaceImage { get; protected set; }

        protected FaceSnapshot() { }

        protected FaceSnapshot(T template, ImmutableImage image)
        {
            FaceImage = image;
            Template = template;
        }

        protected const string NoImage = "NO_IMAGE";

        [XmlElement("Image")]
        public string XmlImage
        {
            get
            {
                if (FaceImage == null) return NoImage;
                using (var ms = new MemoryStream())
                {
                    FaceImage.ToBitmap().Save(ms, ImageFormat.Png);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }

            set
            {
                if (value == NoImage)
                {
                    FaceImage = null;
                    return;
                }

                var bytes = Convert.FromBase64String(value);
                using (var ms = new MemoryStream(bytes))
                {
                    var image = new Bitmap(ms);
                    FaceImage = new ImmutableImage(image);
                }

            }
        }

        [XmlIgnore]
        public abstract string XmlTemplate { get; set; }
    }
}

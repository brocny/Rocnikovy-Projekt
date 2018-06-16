using System.Drawing;
using System.Globalization;
using System.Linq;
using Core.Face;
using Core;
using Core.Kinect;
using Luxand;

namespace FsdkFaceLib
{
    public class FSDKFaceImage
    {
        public int ImageHandle;
        public ImageBuffer ImageBuffer;

        /// <summary>
        /// Location of the top-left of face rectangle in the original frame
        /// </summary>
        public Point OrigLocation;
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] Features;
        public Point[] GetFacialFeatures() => _facialFeatures ?? (_facialFeatures = Features.Select(x => x.ToPoint() + new Size(OrigLocation)).ToArray());
        public long TrackingId;

        private Point[] _facialFeatures;
        public byte[] GetFaceTemplate()
        {
            byte[] retValue;
            if (FacePosition == null)
            {
                FSDK.GetFaceTemplate(ImageHandle, out retValue);
            }
            else if (Features?.Length == FSDK.FSDK_FACIAL_FEATURE_COUNT)
            {
                FSDK.GetFaceTemplateUsingFeatures(ImageHandle, ref Features, out retValue);
            }
            else
            {
                FSDK.GetFaceTemplateInRegion(ImageHandle, ref FacePosition, out retValue);
            }

            return retValue;
        }

        public void DetectFeatures()
        {
            if (FacePosition == null)
            {
                FSDK.DetectFacialFeatures(ImageHandle, out Features);
            }
            else
            {
                FSDK.DetectFacialFeaturesInRegion(ImageHandle, ref FacePosition, out Features);
            }
        }

        /// <summary>
        /// Get the approximate age of the person this face belongs to
        /// </summary>
        /// <returns>The age of the person, null if failed</returns>
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public float? GetAge()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Age", out string response, 256))
            {
                return null;
            }

            string age = response.Split('=', ';')[1];
            if (float.TryParse(age, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ret))
            {
                return ret;
            }

            return null;
        }

        /// <summary>
        /// Get how confident we are that the face belongs to a male
        /// </summary>
        /// <returns>A number between 0 and 1 indicating our confidence that the face is male, null if failed</returns>
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public float? GetConfidenceMale()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Gender", out string response, 128))
            {
                return null;
            }

            if (float.TryParse(response.Split('=', ';')[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ret))
            {
                return ret;
            }

            return null;
        }

        /// <summary>
        /// Get how confident we are that the face belongs to a female
        /// </summary>
        /// <returns>A number between 0 and 1 indicating our confidence that the face is female, null if failed</returns>
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public float? GetConfidenceFemale()
        {
            return 1f - GetConfidenceMale();
        }

        /// <summary>
        /// Get the most likely gender and confidence
        /// </summary>
        /// <returns>Face's gender and confidence (0 to 1)</returns>
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public (Gender gender, float confidence) GetGender()
        {
            var confMale = GetConfidenceMale();
            if (confMale == null) return (Gender.Unknown, 0f);

            if (confMale > 0.5) return (Gender.Male, confMale.Value);
            return (Gender.Female, 1 - confMale.Value);
        }

        /// <summary>
        /// Get how much the face is smiling
        /// </summary>
        /// <returns>A number between 0 and 1 indicating how much the person is smiling, null if failed</returns>
        /// <remarks>
        /// The face's Features need to have been detected beforehand using DetectFeatures(), otherwise null will be returned
        /// Use <see cref="GetExpression"/> if detecting multiple attrbibutes
        /// </remarks>
        public float? GetSmile()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out string response, 128))
            {
                return null;
            }

            if (float.TryParse(response.Split('=', ';')[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ret))
            {
                return ret;
            }

            return null;
        }

        /// <summary>
        /// Get how much the face's eyes are open
        /// </summary>
        /// <returns>A number between 0 and 1 indicating how much the face's eyes are open, null if failed</returns>
        /// <remarks>
        /// The face's Features need to have been detected beforehand using DetectFeatures(), otherwise null will be returned
        /// Use <see cref="GetExpression"/> if detecting multiple attrbibutes
        /// </remarks>
        public float? GetEyesOpen()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out string response, 128))
            {
                return null;
            }

            string eyesOpen = response.Split('=', ';')[3];
            if (float.TryParse(eyesOpen, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ret))
            {
                return ret;
            }

            return null;
        }

        /// <summary>
        /// Get how much the eyes are open, and how the much the person is smiling 
        /// </summary>
        /// <returns>A number between 0 and 1 for each of the tuple</returns>
        /// <remarks>The face's features need to be already detected using DetectFeatures, otherwise (null, null) will be returned</remarks>
        public (float? eyesOpen, float? smile) GetExpression()
        {
            if (Features == null) return (null, null);

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out string response, 128))
            {
                return (null, null);
            }

            var splitResponse = response.Split('=', ';');

            float? eyes = null;
            float? smile = null;

            string eyesString = splitResponse[3];
            if (float.TryParse(eyesString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float eyesTemp))
            {
                eyes = eyesTemp;
            }

            string smileString = splitResponse[1];
            if (float.TryParse(smileString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float smileTemp))
            {
                smile = smileTemp;
            }

            return (eyes, smile);
        }
    }

    public class FaceLocationResult
    {
        public ImageBuffer ImageBuffer;

        public Rectangle[] FaceRectangles;
        public long[] TrackingIds;
        public IBody[] Bodies;
    }

    public class FaceCutout
    {
        /// <summary>
        /// Bitmap image of the face
        /// </summary>
        public ImageBuffer ImageBuffer;

        /// <summary>
        /// Original location of the top-left point of the face rectangle in the original image
        /// </summary>
        public Point OrigLocation;
        public long TrackingId;
    }

    public class FaceTemplate : IFaceTemplate<byte[]>
    {
        public byte[] Template { get; internal set; }
        public ImageBuffer FaceImage { get; internal set; }
        public float Age { get; internal set; }
        public Gender Gender { get; internal set; }
        public float GenderConfidence { get; internal set; }
        public long TrackingId { get; internal set; }
    }
}
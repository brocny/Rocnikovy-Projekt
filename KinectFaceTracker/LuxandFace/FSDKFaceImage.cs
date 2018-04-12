using System.Drawing;
using System.Globalization;
using System.Linq;
using Face;
using KinectUnifier;
using Luxand;

namespace LuxandFaceLib
{
    public class FSDKFaceImage
    {
        public int ImageHandle;


        public ImmutableImage Image;

        /// <summary>
        /// Location of the top-left of face rectangle in the original frame
        /// </summary>
        public Point OrigLocation;
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] Features;
        public Point[] GetFacialFeatures() => Features.Select(x => x.ToPoint() + new Size(OrigLocation)).ToArray();
        public long TrackingId;

        public byte[] GetFaceTemplate()
        {
            byte[] retValue;
            if (Features?.Length == FSDK.FSDK_FACIAL_FEATURE_COUNT)
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
            FSDK.DetectFacialFeaturesInRegion(ImageHandle, ref FacePosition, out Features);
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
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Age", out var response, 256))
            {
                return null;
            }

            var age = response.Split('=', ';')[1];
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
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Gender", out var response, 128))
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
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public float? GetSmile()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out var response, 128))
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
        /// <remarks>The face's Features need to have been detected beforehand using DetectFeatures()</remarks>
        public float? GetEyesOpen()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out var response, 128))
            {
                return null;
            }

            var eyesOpen = response.Split('=', ';')[3];
            if (float.TryParse(eyesOpen, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float ret))
            {
                return ret;
            }

            return null;
        }

        public (float? eyesOpen, float? smile) GetExpression()
        {
            if (Features == null) return (null,null);

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out var response, 128))
            {
                return (null, null);
            }

            var splitResponse = response.Split('=', ';');

            float? eyes = null;
            float? smile = null;

            var eyesString = splitResponse[3];
            if (float.TryParse(eyesString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float eyesTemp))
            {
                eyes = eyesTemp;
            }

            var smileString = splitResponse[1];
            if (float.TryParse(smileString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out float smileTemp))
            {
                smile = smileTemp;
            }

            return (eyes, smile);
        }
    }
}
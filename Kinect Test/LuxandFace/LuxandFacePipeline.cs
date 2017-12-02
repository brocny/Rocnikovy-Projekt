using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;
using LuxandFaceLib;

namespace LuxandFaceLib
{
    public class LuxandFacePipeline
    {
        public IReadOnlyDictionary<int, int> TrackedFaces => _trackedFaces;
        public Task<FaceLocationResult> FaceLocationTask { get; private set; }
        public TaskScheduler SynchContext { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public float SameFaceConfidenceThreshold { get; set; } = 0.95f;
        public float SameFaceNewTemplateThreshold { get; set; } = 0.87f;

        public static string ActivationKey { get; set; } =
            @"qkCo6wATHvarVxeIrN1PI/b1aBxCe1GYdqhGmWEQ3VQqEmjgBtGUDBn5oyu9DqUSxsI4YABRzKDYQ/7Y0MCARdMJs7bgxBt7npmXidPq/4qPgC6bzQZ/bzk9VJBtMBQ08c8T6855C5NDnw8L3QybU+Ou0tnmMN3CtM8mhjQCtvQ=";


        private static bool _isLibraryActivated;
        public static void InitializeLibrary()
        {
            if (_isLibraryActivated) return;
            if (FSDK.FSDKE_OK != FSDK.ActivateLibrary(ActivationKey))
            {
                throw new ApplicationException("Invalid Luxand FSDK activation key");
            }
            _isLibraryActivated = true;
        }

        public LuxandFacePipeline(FaceDatabase<byte[]> db, IDictionary<int, int> trackedFaces = null)
        {
            _faceDb = db;
            _trackedFaces = trackedFaces == null ? new Dictionary<int, int>() : new Dictionary<int, int>(trackedFaces);
        }

        private FaceDatabase<byte[]> _faceDb;
        private Dictionary<int, int> _trackedFaces;

        public Task<FaceLocationResult> LocateFacesAsync(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper)
        {
            FaceLocationTask = Task<FaceLocationResult>.Factory.StartNew(() =>
            {
                var colorTask = Task.Run(() =>
                {
                    var buffer = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyFramePixelDataToArray(buffer);
                    colorFrame.Dispose();
                    return buffer;
                }, CancellationToken);

                var bodyTask = Task.Run(() =>
                {
                    var bodyCount = bodyFrame.BodyCount;
                    var faceRects = new List<Rectangle>(bodyCount);
                    var faceIds = new List<long>(bodyCount);
                    var bodies = new IBody[bodyCount];
                    bodyFrame.CopyBodiesTo(bodies);
                    for (int i = 0; i < bodyCount; i++)
                    {
                        if (Util.TryGetHeadRectangle(bodies[i], mapper, out var faceRect))
                        {
                            faceRects.Add(faceRect);
                            faceIds.Add(bodies[i].TrackingId);
                        }
                    }
                    return (faceRects.ToArray(), faceIds.ToArray());
                }, CancellationToken);
                return new FaceLocationResult
                {
                   BytesPerPixel = 3, ColorBuffer = colorTask.Result, FaceRectangles = bodyTask.Result.Item1, Height = colorFrame.BytesPerPixel, Width = colorFrame.Width
                };

            }, CancellationToken);
            return FaceLocationTask;
        }

        public Task<FaceImage[]> ExtractFaceImagesAsync(Task<FaceLocationResult> fTask)
        {

            return fTask.ContinueWith(t =>
            {
                var fResult = fTask.Result;
                var numFaces = fResult.FaceRectangles.Length;
                var result = new FaceImage[numFaces];
                Parallel.For(0, numFaces, i =>
                {
                    result[i].PixelBuffer = fResult.ColorBuffer.GetBufferRect(fResult.Width, fResult.FaceRectangles[i], fResult.BytesPerPixel);
                    result[i].OrigLocation = fResult.FaceRectangles[i].Location;
                    result[i].TrackingId = (int)fResult.TrackingIds[i];
                });

                return result;
            }, CancellationToken);
        }

        public Task<FSDKFaceImage> CreateFSDKImageAsync(Task<FaceImage> extTask)
        {
            return extTask.ContinueWith(t =>
            {
                var fResult = t.Result;
                int handle = 0;
                FSDK.LoadImageFromBuffer(ref handle, fResult.PixelBuffer, fResult.Width, fResult.Height,
                    fResult.Width * fResult.BytesPerPixel, LuxandUtil.ImageModeFromBytesPerPixel(fResult.BytesPerPixel));
                return new FSDKFaceImage()
                {
                    ImageHandle = handle,
                    Height = fResult.Height,
                    OrigLocation = fResult.OrigLocation,
                    TrackingId = fResult.TrackingId,
                    Width = fResult.Width
                };
            }, CancellationToken);
        }

        public Task<FSDKFaceImage[]> CreateFSDKImagesAsync(Task<FaceImage[]> extTask)
        {
            return extTask.ContinueWith(t =>
            {
                var fResults = t.Result;
                var results = new FSDKFaceImage[fResults.Length];

                for (var i = 0; i < fResults.Length; i++)
                {
                    var fImage = fResults[i];
                    results[i] = new FSDKFaceImage
                    {
                        Width = fImage.Width,
                        Height = fImage.Height,
                        OrigLocation = fImage.OrigLocation,
                        TrackingId = fImage.TrackingId
                    };
                    FSDK.LoadImageFromBuffer(ref results[i].ImageHandle,
                        fImage.PixelBuffer,
                        fImage.Height,
                        fImage.Width,
                        fImage.Height * fImage.BytesPerPixel,
                        LuxandUtil.ImageModeFromBytesPerPixel(fImage.BytesPerPixel));
                }

                return results;
            }, CancellationToken);
        }

        public Task<FSDKFaceImage[]> DetectFacesTaskAsync(Task<FSDKFaceImage[]> crtTask)
        {
            return crtTask.ContinueWith(t =>
            {
                Parallel.For(0, t.Result.Length, i =>
                {
                    var x = t.Result[i];
                    FSDK.DetectFace(x.ImageHandle, ref x.FacePosition);
                });
                return t.Result;
            }, CancellationToken);
        }

        public Task<Point[]> ExtractFeaturesAsync(Task<FSDKFaceImage> fTask)
        {
            return fTask.ContinueWith(t =>
            {
                var fResult = t.Result;
                if (FSDK.FSDKE_OK ==
                    FSDK.DetectFacialFeaturesInRegion(fResult.ImageHandle, ref fResult.FacePosition, out var result))
                {
                    return result.Select(x => x.ToPoint()).ToArray();
                }

                return Array.Empty<Point>();
            }, CancellationToken);
        }

        public Task AddFeaturesTaskAsync(Task<FSDKFaceImage> fTask)
        {
            return fTask.ContinueWith(t =>
            {
                FSDK.DetectFacialFeaturesInRegion(t.Result.ImageHandle, ref t.Result.FacePosition, out t.Result.Features);
            }, CancellationToken);
        }

        public Task<FaceTemplate> ExtractTemplateAsync(Task<FSDKFaceImage> fTask)
        {
            return fTask.ContinueWith(t =>
            {
                var fResult = fTask.Result;
                var result = new FaceTemplate {TrackingId = fResult.TrackingId};
                if (fResult.Features.Length == FSDK.FSDK_FACIAL_FEATURE_COUNT)
                {
                    FSDK.GetFaceTemplateUsingFeatures(fResult.ImageHandle, ref fResult.Features, out result.Template);
                }
                else
                {
                    FSDK.GetFaceTemplate(fResult.ImageHandle, out result.Template);
                }

                return result;
            }, CancellationToken);
        }

        public Task ProccesTemplateAsync(Task<FaceTemplate> fTask)
        {
            return fTask.ContinueWith(t =>
            {
                var res = fTask.Result;
                var match = _faceDb.GetBestMatch(res.Template);
                if (match.confidence >= SameFaceConfidenceThreshold)
                {
                    _trackedFaces[res.TrackingId] = match.id;
                }
                else if (match.confidence >= SameFaceNewTemplateThreshold)
                {
                    _faceDb.TryAddFaceTemplateToExistingFace(match.id, res.Template);
                    _trackedFaces[res.TrackingId] = match.id;
                }
                else
                {
                    var id = _faceDb.NextId;
                    _faceDb.TryAddNewFace(id, res.Template);
                    _trackedFaces[res.TrackingId] = id;
                }
            }, CancellationToken);
        }
    }

    public class FaceLocationResult
    {
        public int Width;
        public int Height;
        public byte[] ColorBuffer;
        public Rectangle[] FaceRectangles;
        public long[] TrackingIds;
        public int BytesPerPixel;
    }

    public class FSDKFaceImage
    {
        public int ImageHandle;
        public int Width;
        public int Height;
        public Point OrigLocation;
        public FSDK.TFacePosition FacePosition;
        public FSDK.TPoint[] Features;
        public Point[] GetFacialFeatures() => Features.Select(x => x.ToPoint() + new Size(OrigLocation)).ToArray();
        public int TrackingId;
    }

    public class FaceImage
    {
        public byte[] PixelBuffer;
        public int BytesPerPixel;
        public Point OrigLocation;
        public int TrackingId;
        public int Height;
        public int Width;
    }

    public class FaceTemplate
    {
        public byte[] Template;
        public int TrackingId;
    }
}
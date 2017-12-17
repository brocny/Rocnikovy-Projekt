using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Face;
using KinectUnifier;
using Luxand;
using System.Threading.Tasks.Dataflow;

namespace LuxandFaceLib
{
    public class LuxandFacePipeline
    {
        /// <summary>
        /// Guaranteed thread-safety
        /// </summary>
        public IReadOnlyDictionary<long, int> TrackedFaces => _trackedFaces;

        public event EventHandler<FaceImage[]> FaceCuttingComplete;
        public event EventHandler<FSDKFaceImage[]> FsdkImageCreationComplete;
        public event EventHandler<FSDKFaceImage[]> FaceDetectionComplete;
        public event EventHandler<FSDKFaceImage[]> FacialFeatureRecognitionComplete;
        public event EventHandler<FaceTemplate[]> FaceTemplateExtractionComplete;
        public event EventHandler<(long trackingId, (int faceId, float confidence) match)[]> TemplateProcessingComplete; 

        public TaskScheduler SynchContext { get; set; }
        public CancellationToken CancellationToken { get; set; }

        public int FSDKInternalResizeWidth
        {
            get { return _internalResizeWidth; }
            set
            {
                _internalResizeWidth = value;
                SetFSDKParams();
            }
        }

        public bool FSDKDetermineLocation
        {
            get { return _determineRotAngle; }
            set
            {
                _determineRotAngle = value;
                SetFSDKParams();
            }
        }

        public bool FSDKHandleArbitrayRot
        {
            get { return _handleArbitrayRot; }
            set
            {
                _handleArbitrayRot = value;
                SetFSDKParams();
            }
        }

        public float SameFaceConfidenceThreshold { get; set; } = 0.92f;
        public float UntrackedFaceNewTemplateThreshold { get; set; } = 0.7f;
        public float TrackedFaceNewTemplateThreshold = 0.35f;

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
            FSDK.InitializeLibrary();
            _isLibraryActivated = true;
        }

        public LuxandFacePipeline(FaceDatabase<byte[]> db = null, TaskScheduler taskScheduler = null, IDictionary<long, int> trackedFaces = null)
        {
            _faceDb = db ?? new FaceDatabase<byte[]>();
            _trackedFaces = trackedFaces == null ? new ConcurrentDictionary<long, int>() : new ConcurrentDictionary<long, int>(trackedFaces);

            var options = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, TaskScheduler = taskScheduler ?? TaskScheduler.Default, MaxDegreeOfParallelism = 2};

            _faceCuttingBlock = new TransformBlock<FaceLocationResult, FaceImage[]>(new Func<FaceLocationResult, FaceImage[]>(CreateFaceImageCutouts), options);
            _fsdkImageCreatingBlock = new TransformBlock<FaceImage[], FSDKFaceImage[]>(new Func<FaceImage[], FSDKFaceImage[]>(CreateFSDKImages), options);
            _faceDetectionBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFaces), options);
            _facialFeaturesBlock = new TransformBlock<FSDKFaceImage[], FSDKFaceImage[]>(new Func<FSDKFaceImage[], FSDKFaceImage[]>(DetectFacialFeatures), options);
            _templateExtractionBlock = new TransformBlock<FSDKFaceImage[], FaceTemplate[]>(new Func<FSDKFaceImage[], FaceTemplate[]>(ExtractTemplates), options);
            _templateProcessingBlock = new ActionBlock<FaceTemplate[]>(new Action<FaceTemplate[]>(ProcessTemplates), options);

            var nullBlock = new ActionBlock<object>(o => {});
            
            _faceCuttingBlock.LinkTo(_fsdkImageCreatingBlock, obj => obj != null);
            _faceCuttingBlock.LinkTo(nullBlock, obj => obj == null);
            _fsdkImageCreatingBlock.LinkTo(_faceDetectionBlock, obj => obj != null);
            _fsdkImageCreatingBlock.LinkTo(nullBlock, obj => obj == null);
            _faceDetectionBlock.LinkTo(_facialFeaturesBlock, obj => obj != null);
            _faceCuttingBlock.LinkTo(nullBlock, obj => obj == null);
            _facialFeaturesBlock.LinkTo(_templateExtractionBlock, obj => obj != null);
            _facialFeaturesBlock.LinkTo(nullBlock, obj => obj == null);
            _templateExtractionBlock.LinkTo(_templateProcessingBlock, obj => obj != null);
            _templateExtractionBlock.LinkTo(nullBlock, obj => obj == null);

            SetFSDKParams();
        }

        private FaceDatabase<byte[]> _faceDb;
        private ConcurrentDictionary<long, int> _trackedFaces;

        public Task<FaceLocationResult> LocateFacesAsync(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper)
        {
            var retTask = Task.Run(() =>
            {
                return _faceLocationFunc((colorFrame, bodyFrame, mapper));
            });

            retTask.ContinueWith(t =>
            {
                if (_faceCuttingBlock.InputCount <= 1)
                {
                   _faceCuttingBlock.Post(t.Result);
                }
            });

            return retTask;
        }

        private Func<(IColorFrame colorFrame, IBodyFrame bodyFrame, ICoordinateMapper mapper), FaceLocationResult> _faceLocationFunc =>
        f =>
        {
            var colorFrame = f.colorFrame;
            var bodyFrame = f.bodyFrame;
            var mapper = f.mapper;
            var colorTask = Task.Run(() =>
            {
                var buffer = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyFramePixelDataToArray(buffer);
                return buffer;
            });

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
                return (faceRects.ToArray(), faceIds.ToArray(), bodies);
            });

            return new FaceLocationResult
            {
                BytesPerPixel = colorFrame.BytesPerPixel,
                ColorBuffer = colorTask.Result,
                FaceRectangles = bodyTask.Result.Item1,
                Height = colorFrame.Height,
                Width = colorFrame.Width,
                Bodies =  bodyTask.Result.Item3,
                TrackingIds = bodyTask.Result.Item2
            };
        };


        private FaceImage[] CreateFaceImageCutouts(FaceLocationResult faceLocations) 
        {
            if (faceLocations?.FaceRectangles == null || faceLocations.FaceRectangles.Length == 0)
            {
                return null;
            }

            var numFaces = faceLocations.FaceRectangles.Length;
            var result = new FaceImage[numFaces];
            for(int i = 0; i < numFaces; i++)
            result[i] = new FaceImage
            {
                PixelBuffer = faceLocations.ColorBuffer.GetBufferRect(faceLocations.Width, faceLocations.FaceRectangles[i], faceLocations.BytesPerPixel),
                OrigLocation = faceLocations.FaceRectangles[i].Location,
                TrackingId = faceLocations.TrackingIds[i],
                Height = faceLocations.FaceRectangles[i].Height,
                Width = faceLocations.FaceRectangles[i].Width,
                BytesPerPixel = faceLocations.BytesPerPixel
            };

            FaceCuttingComplete?.Invoke(this, result);
            return result;
        }
        

        private  FSDKFaceImage[] CreateFSDKImages(FaceImage[] faceImages)
        {
            var results = new FSDKFaceImage[faceImages.Length];
            Parallel.For (0, faceImages.Length, i=>
            {
                var fImage = faceImages[i];
                
                results[i] = new FSDKFaceImage
                {
                    Width = fImage.Width,
                    Height = fImage.Height,
                    OrigLocation = fImage.OrigLocation,
                    TrackingId = fImage.TrackingId,
                };

                FSDK.LoadImageFromBuffer(ref results[i].ImageHandle,
                    fImage.PixelBuffer,
                    fImage.Width,
                    fImage.Height,
                    fImage.Width * fImage.BytesPerPixel,
                    LuxandUtil.ImageModeFromBytesPerPixel(fImage.BytesPerPixel));
            });

            FsdkImageCreationComplete?.Invoke(this, results);
            return results;
        }

        private FSDKFaceImage[] DetectFaces(FSDKFaceImage[] fsdkFaceImages)
        {
            Parallel.For(0, fsdkFaceImages.Length, i =>
            {
                FSDK.DetectFace(fsdkFaceImages[i].ImageHandle, ref fsdkFaceImages[i].FacePosition);
            });

            var result = fsdkFaceImages.Where(i => i.FacePosition != null).ToArray();
            FaceDetectionComplete?.Invoke(this, result);
            return result;
        }

        private FSDKFaceImage[] DetectFacialFeatures(FSDKFaceImage[] fsdkFaceImages)
        {
            Parallel.For(0, fsdkFaceImages.Length, i =>
            {
                fsdkFaceImages[i].DetectFeatures();
            });

            FacialFeatureRecognitionComplete?.Invoke(this, fsdkFaceImages);
            return fsdkFaceImages;
        }
        
        private FaceTemplate[] ExtractTemplates(FSDKFaceImage[] f) 
        {
            var results = f.AsParallel()
                   .Select(x => new FaceTemplate { TrackingId = x.TrackingId, Template = x.GetFaceTemplate() })
                   .ToArray();
            FaceTemplateExtractionComplete?.Invoke(this, results);
            return results;
        }

        private void  ProcessTemplates(FaceTemplate[] templates)
        {
            var matchList = new (long trackingId, (int faceId, float confidence) match)[templates.Length];
            for (var i = 0; i < templates.Length; i++)
            {
                FaceTemplate t = templates[i];
                if (_trackedFaces.TryGetValue(t.TrackingId, out var faceId))
                {
                    var faceInfo = _faceDb.GetFaceInfo(faceId);
                    var similarity = faceInfo.GetSimilarity(t.Template);
                    if (similarity > SameFaceConfidenceThreshold)
                    {
                        matchList[i] = (t.TrackingId, (faceId, similarity));
                    }
                    else if (similarity >= TrackedFaceNewTemplateThreshold)
                    {
                        faceInfo.AddTemplate(t.Template);
                        matchList[i] = (t.TrackingId, (faceId, similarity));
                    }
                    else
                    {
                        NewFace();
                    }
                }
                else
                {
                    NewFace();
                }

                void NewFace()
                {
                    var bestMatch = _faceDb.GetBestMatch(t.Template);
                    if (bestMatch.confidence >= SameFaceConfidenceThreshold)
                    {
                        _trackedFaces[t.TrackingId] = bestMatch.id;
                    }
                    else if (bestMatch.confidence >= UntrackedFaceNewTemplateThreshold)
                    {
                        _faceDb.TryAddFaceTemplateToExistingFace(bestMatch.id, t.Template);
                        _trackedFaces[t.TrackingId] = bestMatch.id;
                    }
                    else
                    {
                        var id = _faceDb.NextId;
                        _faceDb.TryAddNewFace(id, t.Template);
                        _trackedFaces[t.TrackingId] = id;
                    }
                    matchList[i] = (t.TrackingId, bestMatch);
                }
            }

            TemplateProcessingComplete?.Invoke(this, matchList);
        }

        private void SetFSDKParams()
        {
            FSDK.SetFaceDetectionParameters(_handleArbitrayRot, _determineRotAngle, _internalResizeWidth);
        }

        private int _internalResizeWidth = 100;
        private bool _handleArbitrayRot = false;
        private bool _determineRotAngle = false;

        private TransformBlock<FaceLocationResult, FaceImage[]> _faceCuttingBlock;
        private TransformBlock<FaceImage[], FSDKFaceImage[]> _fsdkImageCreatingBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _faceDetectionBlock;
        private TransformBlock<FSDKFaceImage[], FSDKFaceImage[]> _facialFeaturesBlock;
        private TransformBlock<FSDKFaceImage[], FaceTemplate[]> _templateExtractionBlock;
        private ActionBlock<FaceTemplate[]> _templateProcessingBlock;
    }



    public class FaceLocationResult
    {
        public int Width;
        public int Height;
        public byte[] ColorBuffer;
        public Rectangle[] FaceRectangles;
        public long[] TrackingIds;
        public int BytesPerPixel;
        public IBody[] Bodies;
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
        public int? GetAge()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Age", out var response, 256))
            {
                return null;
            }

            return int.Parse(response.Split('=')[1]);
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
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Gender", out var response, 256))
            {
                return null;
            }

            return float.Parse(response.Split('=', ';')[1]);
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
        /// Get how much the face is smiling
        /// </summary>
        /// <returns>A number between 0 and 1 indicating how much the person is smiling, null if failed</returns>
        /// <remarks>The face's Features need to have been detected beforehand</remarks>
        public float? GetSmile()
        {
            if (Features == null) return null;

            if (FSDK.FSDKE_OK !=
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out var response, 256))
            {
                return null;
            }

            return float.Parse(response.Split('=', ';')[1]);
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
                FSDK.DetectFacialAttributeUsingFeatures(ImageHandle, ref Features, "Expression", out var response, 256))
            {
                return null;
            }

            return float.Parse(response.Split('=', ';')[3]);
        }
    }

    public class FaceImage
    {
        public byte[] PixelBuffer;
        public int BytesPerPixel;
        public Point OrigLocation;
        public long TrackingId;
        public int Height;
        public int Width;
    }

    public class FaceTemplate
    {
        public byte[] Template;
        public long TrackingId;
    }
}
using SharpDX.Direct3D11;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Perception.Spatial;

namespace Realtime_Hololens_Retexturing.Common
{
    internal class PhysicalCamera
    {
        private const float MinimumCosine = 0.9999f;
        private const int LockTimeout = 100;
        private const long SharedTextureKey = 0L;
        private const float NearPlane = 0.05f;
        private const float FarPlane = 10.0f;
        private Texture2D cameraTexture;
        private SpatialCoordinateSystem CoordinateSystem;
        private Device device;
        private Texture2D deviceTexture;
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;
        private Matrix4x4 ProjectionMatrix;
        private object TransformLock = new object();
        private Matrix4x4 ViewMatrix;
        private Matrix4x4 LastViewMatrix;
        private bool AllowUnstableFrames;
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool Ready { get; private set; }
        public bool Stable { get; private set; }
        public Vector4 Forward { get; private set; }

        public delegate void OnFrameUpdated();
        public event OnFrameUpdated FrameUpdated;

        public PhysicalCamera(Device device, bool allowUnstableFrames)
        {
            this.device = device;
            AllowUnstableFrames = allowUnstableFrames;
            mediaCapture = new MediaCapture();
            FrameUpdated += () => {};
            Width = 1408;
            Height = 792;
        }

        public Texture2D AcquireTexture()
        {
            if (deviceTexture == null)
                return null;
            LockTexture(deviceTexture);
            return deviceTexture;
        }

        public Matrix4x4 GetWorldToCameraMatrix(SpatialCoordinateSystem originCoordinateSystem)
        {
            lock (TransformLock)
            {
                Forward = new Vector4(-Vector3.UnitZ, 0.0f);
                if (CoordinateSystem == null)
                    return Matrix4x4.Identity;
                var transform = originCoordinateSystem.TryGetTransformTo(CoordinateSystem) ?? Matrix4x4.Identity;
                Matrix4x4.Invert(transform * ViewMatrix, out var inverseMatrix);
                Forward = Vector4.Transform(Forward, inverseMatrix);
                return transform * ViewMatrix * ProjectionMatrix;
            }
        }

        public async void Initialize()
        {
            var sourceGroups = await MediaFrameSourceGroup.FindAllAsync();
            var desiredGroupInfo = sourceGroups.Select(sourceGroup => new
            {
                Group = sourceGroup,
                Info = sourceGroup.SourceInfos.FirstOrDefault(info => info.MediaStreamType == MediaStreamType.VideoPreview && info.SourceKind == MediaFrameSourceKind.Color)
            }).FirstOrDefault(groupInfo => groupInfo.Info != null);
            if (desiredGroupInfo == null)
                return;

            var settings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = desiredGroupInfo.Group,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Auto,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };
            await mediaCapture.InitializeAsync(settings);

            var frameSource = mediaCapture.FrameSources[desiredGroupInfo.Info.Id];
            var preferredFormat = frameSource.SupportedFormats
                .OrderByDescending(format => format.VideoFormat.Width)
                .ThenByDescending(format => (float)format.FrameRate.Numerator / format.FrameRate.Denominator)
                .FirstOrDefault();
            if (preferredFormat == null)
                return;
            await frameSource.SetFormatAsync(preferredFormat);
            var cameraController = frameSource.Controller.VideoDeviceController;
            cameraController.WhiteBalance.TrySetAuto(false);
            cameraController.WhiteBalance.TrySetValue(2600);
            cameraController.Exposure.TrySetAuto(false);
            cameraController.Exposure.TrySetValue(1.0);
            cameraController.BacklightCompensation.TrySetAuto(false);
            cameraController.DesiredOptimization = Windows.Media.Devices.MediaCaptureOptimization.Quality;
            mediaFrameReader = await mediaCapture.CreateFrameReaderAsync(frameSource);
            mediaFrameReader.FrameArrived += OnFrameArrived;
            await mediaFrameReader.StartAsync();
        }

        public void ReleaseTexture()
        {
            UnlockTexture(deviceTexture);
        }

        private void OnFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var reference = sender.TryAcquireLatestFrame();
            lock (TransformLock)
            {
                
                if (reference.Properties.TryGetValue(InteropStatics.MFSampleExtensionSpatialCameraCoordinateSystem, out object coordinateSystem))
                {
                    CoordinateSystem = coordinateSystem as SpatialCoordinateSystem;
                }
                else
                {
                    return;
                }
                var newViewMatrix = (reference.Properties[InteropStatics.MFSampleExtensionSpatialCameraViewTransform] as byte[]).To<Matrix4x4>();
                ProjectionMatrix = (reference.Properties[InteropStatics.MFSampleExtensionSpatialCameraProjectionTransform] as byte[]).To<Matrix4x4>();
                ProjectionMatrix.M33 = FarPlane / (NearPlane - FarPlane);
                ProjectionMatrix.M43 = NearPlane * FarPlane / (NearPlane - FarPlane);
                UpdateStability(newViewMatrix);
                LastViewMatrix = newViewMatrix;
            }
            if (AllowUnstableFrames || Stable)
            {
                ViewMatrix = LastViewMatrix;
                var surface = reference.VideoMediaFrame.Direct3DSurface;
                var surfaceInterfaceAccess = surface as InteropStatics.IDirect3DDxgiInterfaceAccess;
                IntPtr resourcePointer = surfaceInterfaceAccess.GetInterface(InteropStatics.ID3D11Resource);
                Resource resource = SharpDX.CppObject.FromPointer<Resource>(resourcePointer);
                Marshal.Release(resourcePointer);
                Texture2D frameTexture = resource.QueryInterface<Texture2D>();
                if (deviceTexture == null)
                {
                    Texture2D texture = new Texture2D(frameTexture.Device, new Texture2DDescription()
                    {
                        Width = frameTexture.Description.Width,
                        Height = frameTexture.Description.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = frameTexture.Description.Format,
                        SampleDescription = frameTexture.Description.SampleDescription,
                        Usage = frameTexture.Description.Usage,
                        BindFlags = BindFlags.ShaderResource,
                        CpuAccessFlags = CpuAccessFlags.None,
                        OptionFlags = ResourceOptionFlags.SharedKeyedmutex
                    });
                    cameraTexture = frameTexture.Device.OpenSharedResource<Texture2D>(texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle);
                    deviceTexture = device.OpenSharedResource<Texture2D>(texture.QueryInterface<SharpDX.DXGI.Resource>().SharedHandle);
                    Ready = true;
                }
                LockTexture(cameraTexture);
                frameTexture.Device.ImmediateContext.CopyResource(frameTexture, cameraTexture);
                UnlockTexture(cameraTexture);
                FrameUpdated();
            }
        }

        private void UpdateStability(Matrix4x4 newViewMatrix)
        {
            var samplePoint = new Vector3(0.0f, 0.0f, -0.1f);
            Matrix4x4.Invert(LastViewMatrix, out Matrix4x4 oldViewToWorld);
            var transformedSamplePoint = Vector3.Transform(samplePoint, newViewMatrix * oldViewToWorld);
            var cosine = Vector3.Dot(Vector3.Normalize(transformedSamplePoint), Vector3.Normalize(samplePoint));
            Stable = cosine > MinimumCosine;
        }

        #region KeyedMutex Convenience Functions

        private void LockTexture(Texture2D texture) => texture.QueryInterface<SharpDX.DXGI.KeyedMutex>().Acquire(SharedTextureKey, LockTimeout);

        private void UnlockTexture(Texture2D texture) => texture.QueryInterface<SharpDX.DXGI.KeyedMutex>().Release(SharedTextureKey);

        #endregion KeyedMutex Convenience Functions
    }
}
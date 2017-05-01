using SharpDX.Direct3D11;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;

namespace Realistic_Hololens_Rendering.Common
{
    internal class PhysicalCamera
    {
        private const int LockTimeout = 100;
        private const long SharedTextureKey = 0L;
        private Texture2D cameraTexture;
        private Device device;
        private Texture2D deviceTexture;
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;
        public bool Ready { get; private set; }

        public PhysicalCamera(Device device)
        {
            this.device = device;
            mediaCapture = new MediaCapture();
        }

        public Texture2D AcquireTexture()
        {
            LockTexture(deviceTexture);
            return deviceTexture;
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
        }

        #region KeyedMutex Convenience Functions

        private void LockTexture(Texture2D texture) => texture.QueryInterface<SharpDX.DXGI.KeyedMutex>().Acquire(SharedTextureKey, LockTimeout);

        private void UnlockTexture(Texture2D texture) => texture.QueryInterface<SharpDX.DXGI.KeyedMutex>().Release(SharedTextureKey);

        #endregion KeyedMutex Convenience Functions
    }
}
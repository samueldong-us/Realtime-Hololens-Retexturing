using Realistic_Hololens_Rendering.Common;
using SharpDX.Direct3D11;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace Realistic_Hololens_Rendering.Content
{
    internal class MeshTestRenderer : Disposer
    {
        private VertexShader VertexShader;
        private PixelShader PixelShader;
        private VertexShader CubeVertexShader;
        private PixelShader CubePixelShader;
        private SharpDX.Direct3D11.Buffer CameraConstantBuffer;
        private SharpDX.Direct3D11.Buffer CameraDirectionConstantBuffer;

        private bool Active;
        private DeviceResources DeviceResources;
        private MeshCollection Meshes;
        private SpatialSurfaceObserver SurfaceObserver;
        private PhysicalCamera PhysicalCamera;
        private CameraConstantBuffer CameraData = new CameraConstantBuffer();
        private Vector4ConstantBuffer CameraDirectionData = new Vector4ConstantBuffer();
        private SpatialCoordinateSystem CoordinateSystem;
        private RenderableCubemap CubeMap;

        private bool CubeMapUpdateRequired;
        public bool Paused { get; set; }

        public MeshTestRenderer(DeviceResources deviceResources, PhysicalCamera physicalCamera)
        {
            DeviceResources = deviceResources;
            PhysicalCamera = physicalCamera;
            PhysicalCamera.FrameUpdated += OnSteadyFrameAvailable;
            Active = false;
            CubeMapUpdateRequired = false;
            Paused = false;
        }

        private void OnSteadyFrameAvailable()
        {
            CubeMapUpdateRequired = true;
        }

        public async void Initialize(SpatialCoordinateSystem coordinateSystem)
        {
            CoordinateSystem = coordinateSystem;
            var requestStatus = await SpatialSurfaceObserver.RequestAccessAsync();
            if (requestStatus == SpatialPerceptionAccessStatus.Allowed)
            {
                SurfaceObserver = new SpatialSurfaceObserver();
                var boundingBox = new SpatialBoundingBox()
                {
                    Center = Vector3.Zero,
                    Extents = new Vector3(10.0f, 10.0f, 2.5f)
                };
                SurfaceObserver.SetBoundingVolume(SpatialBoundingVolume.FromBox(coordinateSystem, boundingBox));
                await CreateDeviceDenpendantResources();
                SurfaceObserver.ObservedSurfacesChanged += OnObservedSurfacesChanged;
                Active = true;
            }
        }

        public void UpdateTransform(SpatialCoordinateSystem coordinateSystem)
        {
            if (!Active)
                return;

            CoordinateSystem = coordinateSystem;
            Meshes.UpdateTransform(coordinateSystem);
        }

        public void Render()
        {
            if (!Active)
                return;

            var device = DeviceResources.D3DDevice;
            var context = DeviceResources.D3DDeviceContext;

            context.VertexShader.Set(VertexShader);
            context.PixelShader.Set(PixelShader);
            context.PixelShader.SetShaderResource(0, CubeMap.ShaderResourceView);

            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexedInstanced(numberOfIndices, 2, 0, 0, 0);
            });

            context.PixelShader.SetShaderResource(0, null);

            if (CubeMapUpdateRequired && !Paused)
            {
                CameraData.ViewProjection = Matrix4x4.Transpose(PhysicalCamera.GetWorldToCameraMatrix(CoordinateSystem));
                context.UpdateSubresource(ref CameraData, CameraConstantBuffer);
                CameraDirectionData.Vector = PhysicalCamera.Forward;
                context.UpdateSubresource(ref CameraDirectionData, CameraDirectionConstantBuffer);

                context.VertexShader.Set(CubeVertexShader);
                context.VertexShader.SetConstantBuffer(1, CubeMap.CubeArrayBuffer);
                context.VertexShader.SetConstantBuffer(2, CameraConstantBuffer);
                context.PixelShader.Set(CubePixelShader);
                var cameraTexture = PhysicalCamera.AcquireTexture();
                if (cameraTexture == null)
                    return;
                var luminanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription()
                {
                    Format = SharpDX.DXGI.Format.R8_UInt,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                    {
                        MipLevels = 1
                    }
                });
                var chrominanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription()
                {
                    Format = SharpDX.DXGI.Format.R8G8_UInt,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                    {
                        MipLevels = 1
                    }
                });
                context.PixelShader.SetShaderResource(0, luminanceView);
                context.PixelShader.SetShaderResource(1, chrominanceView);
                context.PixelShader.SetConstantBuffer(3, CameraDirectionConstantBuffer);

                context.Rasterizer.SetViewport(0.0f, 0.0f, RenderableCubemap.Resolution, RenderableCubemap.Resolution);
                context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription()
                {
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid
                });
                var blendDescription = new BlendStateDescription();
                for (int i = 0; i < 6; i++)
                {
                    blendDescription.RenderTarget[i].IsBlendEnabled = true;
                    blendDescription.RenderTarget[i].SourceBlend = BlendOption.SourceAlpha;
                    blendDescription.RenderTarget[i].DestinationBlend = BlendOption.InverseSourceAlpha;
                    blendDescription.RenderTarget[i].SourceAlphaBlend = BlendOption.One;
                    blendDescription.RenderTarget[i].DestinationAlphaBlend = BlendOption.Zero;
                    blendDescription.RenderTarget[i].BlendOperation = BlendOperation.Add;
                    blendDescription.RenderTarget[i].AlphaBlendOperation = BlendOperation.Add;
                    blendDescription.RenderTarget[i].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                }
                context.OutputMerger.BlendState = new BlendState(device, blendDescription);
                context.OutputMerger.SetRenderTargets(CubeMap.DepthStencilView, CubeMap.RenderTargetView);
                context.ClearDepthStencilView(CubeMap.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

                Meshes.Draw(numberOfIndices =>
                {
                    context.DrawIndexedInstanced(numberOfIndices, 6, 0, 0, 0);
                });

                luminanceView.Dispose();
                chrominanceView.Dispose();
                PhysicalCamera.ReleaseTexture();

                CubeMapUpdateRequired = false;
            }
        }

        private async Task CreateDeviceDenpendantResources()
        {
            ReleaseDeviceDependentResources();

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var vertexShaderFilename = @"Content\Shaders\Mesh Testing\VprtVertexShader.cso";
            var vertexShaderFile = await folder.GetFileAsync(vertexShaderFilename);
            var vertexShaderBytecode = await DirectXHelper.ReadDataAsync(vertexShaderFile);
            var device = DeviceResources.D3DDevice;
            VertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));

            var pixelShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Testing\PixelShader.cso"));
            PixelShader = ToDispose(new PixelShader(device, pixelShaderBytecode));

            var cubeVertexShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Cube Rendering\VprtVertexShader.cso"));
            CubeVertexShader = ToDispose(new VertexShader(device, cubeVertexShaderBytecode));
            var cubePixelShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Cube Rendering\PixelShader.cso"));
            CubePixelShader = ToDispose(new PixelShader(device, cubePixelShaderBytecode));

            CameraConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref CameraData));
            CameraDirectionConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref CameraDirectionData));

            Meshes = new MeshCollection(DeviceResources, cubeVertexShaderBytecode);
            CubeMap = new RenderableCubemap(DeviceResources, Vector3.Zero);
            CubeMap.Initialize();
        }

        private void ReleaseDeviceDependentResources()
        {
            Active = false;
            RemoveAndDispose(ref VertexShader);
            RemoveAndDispose(ref PixelShader);
            RemoveAndDispose(ref CubeVertexShader);
            RemoveAndDispose(ref CubePixelShader);
            RemoveAndDispose(ref CameraConstantBuffer);
        }

        private void OnObservedSurfacesChanged(SpatialSurfaceObserver sender, object args)
        {
            var observedSurfaces = sender.GetObservedSurfaces();
            Meshes.ProcessSurfaces(observedSurfaces);
        }
    }
}
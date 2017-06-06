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
        private GeometryShader GeometryShader;
        private SharpDX.Direct3D11.Buffer CameraConstantBuffer;
        

        private bool Active;
        private DeviceResources DeviceResources;
        private MeshCollection Meshes;
        private SpatialSurfaceObserver SurfaceObserver;
        private PhysicalCamera PhysicalCamera;
        private CameraConstantBuffer CameraData = new CameraConstantBuffer();
        private SpatialCoordinateSystem CoordinateSystem;

        public MeshTestRenderer(DeviceResources deviceResources, PhysicalCamera physicalCamera)
        {
            DeviceResources = deviceResources;
            PhysicalCamera = physicalCamera;
            Active = false;
        }

        public async Task Initialize(SpatialCoordinateSystem coordinateSystem)
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

            CameraData.ViewProjection = Matrix4x4.Transpose(PhysicalCamera.GetWorldToCameraMatrix(CoordinateSystem));
            context.UpdateSubresource(ref CameraData, CameraConstantBuffer);

            context.VertexShader.Set(VertexShader);
            context.VertexShader.SetConstantBuffer(2, CameraConstantBuffer);
            if (!DeviceResources.D3DDeviceSupportsVprt)
            {
                context.GeometryShader.Set(GeometryShader);
            }
            context.PixelShader.Set(PixelShader);
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

            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexedInstanced(numberOfIndices, 2, 0, 0, 0);
            });

            luminanceView.Dispose();
            chrominanceView.Dispose();
            PhysicalCamera.ReleaseTexture();
        }

        private async Task CreateDeviceDenpendantResources()
        {
            ReleaseDeviceDependentResources();

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var vertexShaderFilename = DeviceResources.D3DDeviceSupportsVprt ? @"Content\Shaders\Mesh Testing\VprtVertexShader.cso" : @"Content\Shaders\Mesh Testing\VertexShader.cso";
            var vertexShaderFile = await folder.GetFileAsync(vertexShaderFilename);
            var vertexShaderBytecode = await DirectXHelper.ReadDataAsync(vertexShaderFile);
            var device = DeviceResources.D3DDevice;
            VertexShader = ToDispose(new VertexShader(device, vertexShaderBytecode));

            if (!DeviceResources.D3DDeviceSupportsVprt)
            {
                var geometryShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Testing\GeometryShader.cso"));
                GeometryShader = ToDispose(new GeometryShader(device, geometryShaderBytecode));
            }

            var pixelShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Testing\PixelShader.cso"));
            PixelShader = ToDispose(new PixelShader(device, pixelShaderBytecode));

            CameraConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref CameraData));

            Meshes = new MeshCollection(DeviceResources, vertexShaderBytecode);
        }

        private void ReleaseDeviceDependentResources()
        {
            Active = false;
            RemoveAndDispose(ref VertexShader);
            RemoveAndDispose(ref GeometryShader);
            RemoveAndDispose(ref PixelShader);
            RemoveAndDispose(ref CameraConstantBuffer);
        }

        private void OnObservedSurfacesChanged(SpatialSurfaceObserver sender, object args)
        {
            var observedSurfaces = sender.GetObservedSurfaces();
            Meshes.ProcessSurfaces(observedSurfaces);
        }
    }
}
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

        private bool Active;
        private DeviceResources DeviceResources;
        private MeshCollection Meshes;
        private SpatialSurfaceObserver SurfaceObserver;

        public MeshTestRenderer(DeviceResources deviceResources)
        {
            DeviceResources = deviceResources;
            Active = false;
        }

        public async Task Initialize(SpatialCoordinateSystem coordinateSystem)
        {
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

            Meshes.UpdateTransform(coordinateSystem);
        }

        public void Render()
        {
            if (!Active)
                return;

            var device = DeviceResources.D3DDevice;
            var context = DeviceResources.D3DDeviceContext;

            context.VertexShader.Set(VertexShader);
            if (!DeviceResources.D3DDeviceSupportsVprt)
            {
                context.GeometryShader.Set(GeometryShader);
            }
            context.PixelShader.Set(PixelShader);

            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexedInstanced(numberOfIndices, 2, 0, 0, 0);
            });
        }

        private async Task CreateDeviceDenpendantResources()
        {
            ReleaseDeviceDependentResources();

            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var vertexShaderFilename = DeviceResources.D3DDeviceSupportsVprt ? @"Content\Shaders\Mesh Testing\VprtVertexShader.cso" : @"Content\Shaders\Mesh Testing\VertexShader.cso";
            var vertexShaderFile = await folder.GetFileAsync(vertexShaderFilename);
            var vertexShaderBytecode = await DirectXHelper.ReadDataAsync(vertexShaderFile);
            VertexShader = ToDispose(new VertexShader(DeviceResources.D3DDevice, vertexShaderBytecode));

            if (!DeviceResources.D3DDeviceSupportsVprt)
            {
                var geometryShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Testing\GeometryShader.cso"));
                GeometryShader = ToDispose(new GeometryShader(DeviceResources.D3DDevice, geometryShaderBytecode));
            }

            var pixelShaderBytecode = await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Testing\PixelShader.cso"));
            PixelShader = ToDispose(new PixelShader(DeviceResources.D3DDevice, pixelShaderBytecode));

            Meshes = new MeshCollection(DeviceResources, vertexShaderBytecode);
        }

        private void ReleaseDeviceDependentResources()
        {
            Active = false;
            RemoveAndDispose(ref VertexShader);
            RemoveAndDispose(ref GeometryShader);
            RemoveAndDispose(ref PixelShader);
        }

        private void OnObservedSurfacesChanged(SpatialSurfaceObserver sender, object args)
        {
            var observedSurfaces = sender.GetObservedSurfaces();
            Meshes.ProcessSurfaces(observedSurfaces);
        }
    }
}
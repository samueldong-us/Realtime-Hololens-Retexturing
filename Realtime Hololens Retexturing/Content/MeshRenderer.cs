using Realtime_Hololens_Retexturing.Common;
using SharpDX.Direct3D11;
using System;
using System.Threading.Tasks;

namespace Realtime_Hololens_Retexturing.Content
{
    internal class MeshRenderer : Disposer
    {
        #region DirectX Objects

        private SharpDX.Direct3D11.Buffer LayoutConstantBuffer;
        private LayoutConstantBuffer LayoutData = new LayoutConstantBuffer();
        private GeometryShader RenderGeometryShader;
        private PixelShader RenderPixelShader;
        private VertexShader RenderVertexShader;

        #endregion DirectX Objects

        private DeviceResources Resources;

        public MeshRenderer(DeviceResources resources)
        {
            Resources = resources;
        }

        public async Task CreateDeviceDependantResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            RenderVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Rendering\VertexShader.cso"));
            RenderGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Rendering\GeometryShader.cso"));
            RenderPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Rendering\PixelShader.cso"));

            LayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref LayoutData));
        }

        public void RenderMesh(MeshCollection meshes, ShaderResourceView texture)
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var newTriangleCount = meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            context.VertexShader.Set(RenderVertexShader);
            context.GeometryShader.Set(RenderGeometryShader);
            context.GeometryShader.SetConstantBuffer(3, LayoutConstantBuffer);
            context.PixelShader.Set(RenderPixelShader);
            context.PixelShader.SetShaderResource(0, texture);
            context.PixelShader.SetSampler(0, new SamplerState(device, new SamplerStateDescription
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Equal,
                Filter = Filter.MinMagLinearMipPoint
            }));

            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            int newOffset = 0;
            meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexedInstanced(numberOfIndices, 2, 0, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                LayoutData.Offset = (uint)newOffset;
                LayoutData.Size = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;
                context.UpdateSubresource(ref LayoutData, LayoutConstantBuffer);
                return true;
            });

            context.PixelShader.SetShaderResource(0, null);
        }
    }
}
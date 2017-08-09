using Realistic_Hololens_Rendering.Content;
using SharpDX.Direct3D11;
using System;
using System.Numerics;

namespace Realistic_Hololens_Rendering.Common
{
    class RenderableCubemap : Disposer
    {
        public const int Resolution = 1024;
        public RenderTargetView RenderTargetView { get; private set; }
        public DepthStencilView DepthStencilView { get; private set; }
        public ShaderResourceView ShaderResourceView { get; private set; }
        private Texture2D Faces;
        private Texture2D FaceDepths;
        private DeviceResources Resources;
        private Vector3 Position;
        public SharpDX.Direct3D11.Buffer CubeArrayBuffer { get; private set; }

        public RenderableCubemap(DeviceResources resources, Vector3 position)
        {
            Resources = resources;
            Position = position;
        }

        public void Initialize()
        {
            CreateDeviceDependentResources();
        }

        public void CreateDeviceDependentResources()
        {
            CreateTexturesAndViews();
            CreateMatrixArrayBuffer();
        }

        private void CreateMatrixArrayBuffer()
        {
            var eyeVectors = new[]
                        {
                Position + Vector3.UnitX,
                Position - Vector3.UnitX,
                Position + Vector3.UnitY,
                Position - Vector3.UnitY,
                Position + Vector3.UnitZ,
                Position - Vector3.UnitZ
            };
            var upVectors = new[]
            {
                Vector3.UnitY,
                Vector3.UnitY,
                -Vector3.UnitZ,
                Vector3.UnitZ,
                Vector3.UnitY,
                Vector3.UnitY
            };
            var viewProjectionMatrices = new CubeArrayBuffer();
            var projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView((float)Math.PI / 2.0f, 1.0f, 0.1f, 1000.0f);
            for (int i = 0; i < 6; i++)
            {
                var viewMatrix = Matrix4x4.CreateLookAt(Position, eyeVectors[i], upVectors[i]) * Matrix4x4.CreateScale(-1.0f, 1.0f, 1.0f);
                viewProjectionMatrices[i] = Matrix4x4.Transpose(viewMatrix * projectionMatrix);
            }
            CubeArrayBuffer = SharpDX.Direct3D11.Buffer.Create(Resources.D3DDevice, BindFlags.ConstantBuffer, ref viewProjectionMatrices);
        }

        private void CreateTexturesAndViews()
        {
            var device = Resources.D3DDevice;

            Faces = ToDispose(new Texture2D(device, new Texture2DDescription()
            {
                Width = Resolution,
                Height = Resolution,
                ArraySize = 6,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.TextureCube,
                MipLevels = 0,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }));

            var renderTargetViewDescription = new RenderTargetViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Dimension = RenderTargetViewDimension.Texture2DArray
            };
            renderTargetViewDescription.Texture2DArray.MipSlice = 0;
            renderTargetViewDescription.Texture2DArray.FirstArraySlice = 0;
            renderTargetViewDescription.Texture2DArray.ArraySize = 6;
            RenderTargetView = ToDispose(new RenderTargetView(device, Faces, renderTargetViewDescription));

            FaceDepths = ToDispose(new Texture2D(device, new Texture2DDescription()
            {
                Width = Resolution,
                Height = Resolution,
                ArraySize = 6,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.DepthStencil,
                Format = SharpDX.DXGI.Format.D32_Float,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.TextureCube,
                MipLevels = 1,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }));

            var depthStencilViewDescription = new DepthStencilViewDescription()
            {
                Format = SharpDX.DXGI.Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2DArray,
                Flags = DepthStencilViewFlags.None
            };
            depthStencilViewDescription.Texture2DArray.ArraySize = 6;
            depthStencilViewDescription.Texture2DArray.FirstArraySlice = 0;
            depthStencilViewDescription.Texture2DArray.MipSlice = 0;
            DepthStencilView = ToDispose(new DepthStencilView(device, FaceDepths, depthStencilViewDescription));

            var shaderResourceViewDescription = new ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.TextureCube
            };
            shaderResourceViewDescription.TextureCube.MipLevels = -1;
            shaderResourceViewDescription.TextureCube.MostDetailedMip = 0;
            ShaderResourceView = ToDispose(new ShaderResourceView(device, Faces, shaderResourceViewDescription));
            
            Resources.D3DDeviceContext.ClearRenderTargetView(RenderTargetView, new SharpDX.Mathematics.Interop.RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
        }
    }
}
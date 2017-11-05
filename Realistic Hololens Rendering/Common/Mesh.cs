using Realistic_Hololens_Rendering.Content;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Numerics;

namespace Realistic_Hololens_Rendering.Common
{
    class Mesh : Disposer
    {
        private SharpDX.Direct3D11.Buffer VertexBuffer;
        private SharpDX.Direct3D11.Buffer ModelConstantBuffer;

        public Vector3 Position
        {
            get => _Position;
            set
            {
                _Position = value;
                UpdateTransform();
            }
        }
        public Vector3 Rotation
        {
            get => _Rotation;
            set
            {
                _Rotation = value;
                UpdateTransform();
            }
        }

        private Vector3 _Position;
        private Vector3 _Rotation;
        private int VertexCount;
        private ModelConstantBuffer ModelConstantBufferData = new ModelConstantBuffer();
        private DeviceResources DeviceResources;

        public static InputLayout GetInputLayout(SharpDX.Direct3D11.Device device, byte[] shaderBytecode)
        {
            InputElement[] inputDescription =
            {
                new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0, InputClassification.PerVertexData, 0),
                new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0, InputClassification.PerVertexData, 0)
            };
            return new InputLayout(device, shaderBytecode, inputDescription);
        }

        public Mesh(DeviceResources deviceResources, VertexPositionNormalUV[] mesh)
        {
            DeviceResources = deviceResources;
            CreateDeviceDependantResources(mesh);
        }

        public void Render(Action<int> drawingFunction)
        {
            var device = DeviceResources.D3DDevice;
            var context = DeviceResources.D3DDeviceContext;

            var vertexBufferBinding = new VertexBufferBinding(VertexBuffer, SharpDX.Utilities.SizeOf<VertexPositionNormalUV>(), 0);
            context.InputAssembler.SetVertexBuffers(0, vertexBufferBinding);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.VertexShader.SetConstantBuffer(0, ModelConstantBuffer);

            drawingFunction(VertexCount);
        }

        private void UpdateTransform()
        {
            var modelRotation = Matrix4x4.CreateFromYawPitchRoll(Rotation.X, Rotation.Y, Rotation.Z);
            var modelTranslation = Matrix4x4.CreateTranslation(Position);

            ModelConstantBufferData.Model = Matrix4x4.Transpose(modelRotation * modelTranslation);
            DeviceResources.D3DDeviceContext.UpdateSubresource(ref ModelConstantBufferData, ModelConstantBuffer);
        }

        private void CreateDeviceDependantResources(VertexPositionNormalUV[] mesh)
        {
            var device = DeviceResources.D3DDevice;
            VertexCount = mesh.Length;
            VertexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, mesh));
            ModelConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref ModelConstantBufferData));
        }
    }
}
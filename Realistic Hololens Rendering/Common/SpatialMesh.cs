using Realistic_Hololens_Rendering.Content;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace Realistic_Hololens_Rendering.Common
{
    internal class SpatialMesh : Disposer
    {
        #region DirectX Objects

        private SharpDX.Direct3D11.Buffer IndexBuffer;
        private InputLayout InputLayout;
        private SharpDX.Direct3D11.Buffer NormalBuffer;
        private SharpDX.Direct3D11.Buffer PositionBuffer;
        private SharpDX.Direct3D11.Buffer TransformConstantBuffer;

        #endregion DirectX Objects

        public int NumberOfIndices { get => (int)(Mesh?.TriangleIndices?.ElementCount ?? 0); }
        public SpatialSurfaceMesh Mesh { get; private set; }

        public TransformConstantBuffer TransformData = new TransformConstantBuffer();
        private bool Ready;
        private object ReadyLock = new object();
        private DeviceResources Resources;

        public SpatialMesh(DeviceResources resources)
        {
            Resources = resources;
            Ready = false;
        }

        public void Draw(Action<int> drawingFunction)
        {
            lock (ReadyLock)
            {
                if (!Ready)
                    return;
            }
            var context = Resources.D3DDeviceContext;

            var positionBufferBinding = new VertexBufferBinding(PositionBuffer, (int)Mesh.VertexPositions.Stride, 0);
            var normalBufferBinding = new VertexBufferBinding(NormalBuffer, (int)Mesh.VertexNormals.Stride, 0);
            context.InputAssembler.SetVertexBuffers(0, positionBufferBinding);
            context.InputAssembler.SetVertexBuffers(1, normalBufferBinding);
            context.InputAssembler.SetIndexBuffer(IndexBuffer, (Format)Mesh.TriangleIndices.Format, 0);
            context.InputAssembler.InputLayout = InputLayout;
            context.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;

            context.VertexShader.SetConstantBuffer(0, TransformConstantBuffer);
            drawingFunction((int)Mesh.TriangleIndices.ElementCount);
        }

        public void ProcessMeshData(SpatialSurfaceInfo surfaceInfo, byte[] vertexShaderBytecode)
        {
            lock (ReadyLock)
            {
                Ready = false;
            }

            if (Mesh != null)
            {
                RemoveMeshData();
            }

            var options = new SpatialSurfaceMeshOptions
            {
                IncludeVertexNormals = true
            };
            Mesh = surfaceInfo.TryComputeLatestMeshAsync(1000.0, options).AsTask().Result;
            if (Mesh == null || Mesh.TriangleIndices.ElementCount < 3)
                return;

            var device = Resources.D3DDevice;
            InputElement[] inputDescription =
            {
                new InputElement("POSITION", 0, (Format)Mesh.VertexPositions.Format, 0, 0, InputClassification.PerVertexData, 0),
                new InputElement("NORMAL", 0, (Format)Mesh.VertexNormals.Format, 0, 1, InputClassification.PerVertexData, 0)
            };
            InputLayout = ToDispose(new InputLayout(device, vertexShaderBytecode, inputDescription));

            PositionBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, Mesh.VertexPositions.Data.ToArray()));
            NormalBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.VertexBuffer, Mesh.VertexNormals.Data.ToArray()));
            IndexBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.IndexBuffer, Mesh.TriangleIndices.Data.ToArray()));

            TransformConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref TransformData));

            lock (ReadyLock)
            {
                Ready = true;
            }
        }
        
        public void RemoveMeshData()
        {
            RemoveAndDispose(ref InputLayout);
            RemoveAndDispose(ref PositionBuffer);
            RemoveAndDispose(ref NormalBuffer);
            RemoveAndDispose(ref IndexBuffer);
            RemoveAndDispose(ref TransformConstantBuffer);

            lock(ReadyLock)
            {
                Ready = false;
            }
        }

        public void UpdateTransform(SpatialCoordinateSystem coordinateSystem)
        {
            lock (ReadyLock)
            {
                if (!Ready)
                    return;
            }

            var transformAttempt = Mesh.CoordinateSystem.TryGetTransformTo(coordinateSystem);
            var transform = transformAttempt ?? Matrix4x4.Identity;
            var transformScale = Matrix4x4.CreateScale(Mesh.VertexPositionScale);
            TransformData.VertexTransform = Matrix4x4.Transpose(transformScale * transform);

            var normalTransform = transform;
            normalTransform.Translation = Vector3.Zero;
            TransformData.NormalTransform = Matrix4x4.Transpose(normalTransform);
            
            var context = Resources.D3DDeviceContext;
            context.UpdateSubresource(ref TransformData, TransformConstantBuffer);
        }
    }
}
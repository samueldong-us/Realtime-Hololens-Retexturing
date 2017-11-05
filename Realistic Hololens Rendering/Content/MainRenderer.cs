using Realistic_Hololens_Rendering.Common;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;
using Windows.Storage;

namespace Realistic_Hololens_Rendering.Content
{
    class MainRenderer : Disposer
    {
        private const int Resolution = 4096;

        private VertexShader RenderVertexShader;
        private GeometryShader RenderGeometryShader;
        private PixelShader RenderPixelShader;

        private SharpDX.Direct3D11.Buffer LayoutConstantBuffer;
        private LayoutConstantBuffer LayoutData = new LayoutConstantBuffer();

        private bool Active;
        private bool ProjectionRequested;
        private bool UpdateRequested;
        private Dictionary<Guid, int> PreviousOffsets;
        private int PreviousCount;
        private Dictionary<Guid, SpatialSurfaceInfo> Surfaces;

        private DeviceResources Resources;
        private PhysicalCamera Camera;
        private MeshCollection Meshes;
        private SpatialCoordinateSystem CoordinateSystem;
        private SpatialSurfaceObserver SurfaceObserver;
        private SpeechRecognizer SpeechRecognizer;

        private TextureDebugRenderer TextureDebugger;
        private MeshTexturer MeshTexturer;

        private bool GeometryPaused;
        private bool CameraPaused;
        private bool Debug;
        private bool ExportRequested;

        public MainRenderer(DeviceResources resources, PhysicalCamera camera)
        {
            Resources = resources;
            Camera = camera;
            Active = false;
            ProjectionRequested = false;
            UpdateRequested = false;
            Debug = false;
            ExportRequested = false;
            TextureDebugger = new TextureDebugRenderer(resources);
            MeshTexturer = new MeshTexturer(resources, camera, Resolution);

            Camera.FrameUpdated += RequestMeshProjection;

            SetupSpeechRecognition();
        }

        private async void SetupSpeechRecognition()
        {
            SpeechRecognizer = new SpeechRecognizer();
            var speechOptions = new SpeechRecognitionListConstraint(new[]
            {
                "Toggle Camera",
                "Toggle Geometry",
                "Toggle Debug",
                "Export Mesh"
            });
            SpeechRecognizer.Constraints.Add(speechOptions);
            var result = await SpeechRecognizer.CompileConstraintsAsync();
            if (result.Status == SpeechRecognitionResultStatus.Success)
            {
                await SpeechRecognizer.ContinuousRecognitionSession.StartAsync();
            }
            SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += OnSpeechCommandDetected;
        }

        private void OnSpeechCommandDetected(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            switch (args.Result.Text)
            {
                case "Toggle Camera":
                    CameraPaused = !CameraPaused;
                    break;
                case "Toggle Geometry":
                    GeometryPaused = !GeometryPaused;
                    break;
                case "Toggle Debug":
                    Debug = !Debug;
                    break;
                case "Export Mesh":
                    ExportRequested = true;
                    break;
            }
        }

        private void ExportMeshAndTexture()
        {
            var timestamp = DateTime.Now.ToString("MMM dd, yyyy - hh-mm-ss");
            var localFolder = ApplicationData.Current.LocalFolder;
            var newFolder = localFolder.CreateFolderAsync(timestamp).AsTask().Result;
            var modelFile = newFolder.CreateFileAsync("Mesh.obj", CreationCollisionOption.ReplaceExisting).AsTask().Result;
            FileIO.WriteTextAsync(modelFile, Meshes.ExportMesh("Material.mtl", "default")).AsTask().Wait(-1);
            var materialFile = newFolder.CreateFileAsync("Material.mtl", CreationCollisionOption.ReplaceExisting).AsTask().Result;
            FileIO.WriteTextAsync(materialFile, MeshExporter.GetMaterialFile("Texture.png")).AsTask().Wait(-1);
            var textureFile = newFolder.CreateFileAsync("Texture.png", CreationCollisionOption.ReplaceExisting).AsTask().Result;
            MeshExporter.ExportTexture(Resources, textureFile, MeshTexturer.MeshColorTexture);
        }
        
        private void RequestMeshProjection()
        {
            ProjectionRequested = true;
        }

        private void RequestPackingUpdate(Dictionary<Guid, int> oldOffsets, int oldCount, Dictionary<Guid, SpatialSurfaceInfo> surfaces)
        {
            if (!UpdateRequested)
            {
                UpdateRequested = true;
                PreviousOffsets = oldOffsets;
                PreviousCount = oldCount;
                Surfaces = surfaces;
            }
        }

        public void UpdateTransform(SpatialCoordinateSystem coordinateSystem)
        {
            CoordinateSystem = coordinateSystem;
            Meshes.UpdateTransform(coordinateSystem);
        }

        public void Render()
        {
            if (!Active)
                return;

            RenderMesh();
            if (Debug)
            {
                TextureDebugger.Render(MeshTexturer.ColorResourceView, new Vector4(-1.0f, -1.0f, 0.5f, 0.5f));
            }
            if (ProjectionRequested && !CameraPaused)
            {
                MeshTexturer.ProjectCameraTexture(Meshes, CoordinateSystem);
                ProjectionRequested = false;
            }
            if (UpdateRequested && !GeometryPaused)
            {
                Meshes.UpdateMesh(Surfaces);
                MeshTexturer.UpdatePacking(Meshes, PreviousCount, PreviousOffsets);
                UpdateRequested = false;
            }
            if (ExportRequested)
            {
                ExportMeshAndTexture();
                ExportRequested = false;
            }
        }

        private void RenderMesh()
        {
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var newTriangleCount = Meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            context.VertexShader.Set(RenderVertexShader);
            context.GeometryShader.Set(RenderGeometryShader);
            context.GeometryShader.SetConstantBuffer(3, LayoutConstantBuffer);
            context.PixelShader.Set(RenderPixelShader);
            context.PixelShader.SetShaderResource(0, MeshTexturer.ColorResourceView);
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
            Meshes.Draw(numberOfIndices =>
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

        public void Initialize(SpatialCoordinateSystem coordinateSystem)
        {
            CoordinateSystem = coordinateSystem;
            CreateDeviceDependentResources();
        }

        private async Task InitializeSurfaceObservation()
        {
            var requestStatus = await SpatialSurfaceObserver.RequestAccessAsync();
            if (requestStatus == SpatialPerceptionAccessStatus.Allowed)
            {
                SurfaceObserver = new SpatialSurfaceObserver();
                var boundingBox = new SpatialBoundingBox
                {
                    Center = Vector3.Zero,
                    Extents = new Vector3(10.0f, 10.0f, 10.0f)
                };
                SurfaceObserver.SetBoundingVolume(SpatialBoundingVolume.FromBox(CoordinateSystem, boundingBox));
                SurfaceObserver.ObservedSurfacesChanged += (sender, _) =>
                {
                    if (!GeometryPaused)
                    {
                        Meshes.ProcessSurfaces(sender.GetObservedSurfaces());
                    }
                };
            }
            MeshTexturer.InitializeTextures();
        }

        public async void CreateDeviceDependentResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            Meshes = new MeshCollection(Resources, await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Updating\VertexShader.cso")));
            Meshes.OnMeshChanged += RequestPackingUpdate;

            RenderVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Rendering\VertexShader.cso"));
            RenderGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Rendering\GeometryShader.cso"));
            RenderPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Rendering\PixelShader.cso"));

            LayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref LayoutData));

            await MeshLoader.LoadObj(Resources, @"Content\Assets\cube rounded.obj");

            await InitializeSurfaceObservation();
            Active = true;
        }
    }
}
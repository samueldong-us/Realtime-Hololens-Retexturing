using Realistic_Hololens_Rendering.Common;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.Mathematics.Interop;
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
    class MeshCollectionTexturer : Disposer
    {
        private const int Resolution = 4096;

        private VertexShader UpdateVertexShader;
        private GeometryShader UpdateGeometryShader;
        private PixelShader UpdatePixelShader;

        private VertexShader PrepassVertexShader;
        private PixelShader PrepassPixelShader;

        private VertexShader ProjectionVertexShader;
        private GeometryShader ProjectionGeometryShader;
        private PixelShader ProjectionPixelShader;

        private VertexShader RenderVertexShader;
        private GeometryShader RenderGeometryShader;
        private PixelShader RenderPixelShader;

        private Texture2D DepthTexture;
        private ShaderResourceView DepthResource;
        private DepthStencilView DepthTarget;

        private SharpDX.Direct3D11.Buffer UpdateLayoutConstantBuffer;
        private UpdateLayoutConstantBuffer UpdateLayoutData = new UpdateLayoutConstantBuffer();
        private SharpDX.Direct3D11.Buffer LayoutConstantBuffer;
        private LayoutConstantBuffer LayoutData = new LayoutConstantBuffer();
        private SharpDX.Direct3D11.Buffer CameraConstantBuffer;
        private CameraConstantBuffer CameraData = new CameraConstantBuffer();

        public RenderTargetView RenderColorView { get => MeshTextures[CurrentTexture].RenderColorView; }
        public RenderTargetView RenderQualityAndTimeView { get => MeshTextures[CurrentTexture].RenderQualityAndTimeView; }
        public ShaderResourceView ColorResourceView { get => MeshTextures[CurrentTexture].ColorResourceView; }
        public ShaderResourceView QualityAndTimeResourceView { get => MeshTextures[CurrentTexture].QualityAndTimeResourceView; }
        private MeshTextureSet[] MeshTextures;
        private int CurrentTexture;

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

        private bool GeometryPaused;
        private bool CameraPaused;
        private bool Debug;

        public MeshCollectionTexturer(DeviceResources resources, PhysicalCamera camera)
        {
            Resources = resources;
            Camera = camera;
            Active = false;
            ProjectionRequested = false;
            UpdateRequested = false;
            Debug = false;
            TextureDebugger = new TextureDebugRenderer(resources);

            Camera.FrameUpdated += RequestMeshProjection;
            MeshTextures = new[]
            {
                ToDispose(new MeshTextureSet(resources, Resolution)),
                ToDispose(new MeshTextureSet(resources, Resolution))
            };
            CurrentTexture = 0;

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

        private async void OnSpeechCommandDetected(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
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
                    await ExportMeshAndTexture();
                    break;
            }
        }

        private async Task ExportMeshAndTexture()
        {
            var timestamp = DateTime.Now.ToString("MMM dd, yyyy - hh:mm:ss");
            var localFolder = ApplicationData.Current.LocalFolder;
            var newFolder = await localFolder.CreateFolderAsync(timestamp);
            var modelFile = await newFolder.CreateFileAsync("Mesh.obj");
            await FileIO.WriteTextAsync(modelFile, Meshes.ExportMesh("Material.mtl", "default"));
            var materialFile = await newFolder.CreateFileAsync("Material.mtl");
            await FileIO.WriteTextAsync(materialFile, GenerateMaterialFile("Texture.png"));
            var textureFile = await newFolder.CreateFileAsync("Texture.png");
            await SaveTexture(textureFile);
        }

        private async Task SaveTexture(StorageFile textureFile)
        {
            throw new NotImplementedException();
        }

        private string GenerateMaterialFile(string textureFile)
        {
            throw new NotImplementedException();
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
                TextureDebugger.Render(MeshTextures[CurrentTexture].ColorResourceView, new Vector4(-1.0f, -1.0f, 0.5f, 0.5f));
                TextureDebugger.Render(MeshTextures[(CurrentTexture + 1) % 2].ColorResourceView, new Vector4(0.5f, -1.0f, 0.5f, 0.5f));
            }
            if (ProjectionRequested && !CameraPaused)
            {
                ProjectCameraTexture();
                ProjectionRequested = false;
            }
            if (UpdateRequested && !GeometryPaused)
            {
                UpdatePacking();
                UpdateRequested = false;
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
            context.PixelShader.SetShaderResource(0, MeshTextures[CurrentTexture].ColorResourceView);
            context.PixelShader.SetSampler(0, new SamplerState(device, new SamplerStateDescription()
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                ComparisonFunction = Comparison.Equal,
                Filter = Filter.MinMagLinearMipPoint
            }));

            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription()
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

        private void ProjectCameraTexture()
        {
            if (!Active)
                return;
            
            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            CameraData.ViewProjection = Matrix4x4.Transpose(Camera.GetWorldToCameraMatrix(CoordinateSystem));
            context.UpdateSubresource(ref CameraData, CameraConstantBuffer);

            context.VertexShader.Set(PrepassVertexShader);
            context.VertexShader.SetConstantBuffer(2, CameraConstantBuffer);
            context.GeometryShader.Set(null);
            context.PixelShader.Set(null);

            context.ClearDepthStencilView(DepthTarget, DepthStencilClearFlags.Depth, 1.0f, 0);
            context.OutputMerger.SetRenderTargets(DepthTarget, (RenderTargetView)null);

            context.Rasterizer.SetViewport(0, 0, Camera.Width, Camera.Height);
            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            });

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);

            var newTriangleCount = Meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            context.VertexShader.Set(ProjectionVertexShader);
            context.GeometryShader.Set(ProjectionGeometryShader);
            context.GeometryShader.SetConstantBuffer(3, LayoutConstantBuffer);
            context.PixelShader.Set(ProjectionPixelShader);
            context.PixelShader.SetConstantBuffer(2, CameraConstantBuffer);
            var cameraTexture = Camera.AcquireTexture();
            if (cameraTexture == null)
            {
                return;
            }
            var luminanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8_UInt,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            var chrominanceView = new ShaderResourceView(device, cameraTexture, new ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R8G8_UInt,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource()
                {
                    MipLevels = 1
                }
            });
            context.PixelShader.SetShaderResource(1, luminanceView);
            context.PixelShader.SetShaderResource(2, chrominanceView);
            context.PixelShader.SetShaderResource(3, DepthResource);

            context.OutputMerger.SetRenderTargets(null, MeshTextures[CurrentTexture].RenderColorView);

            context.Rasterizer.SetViewport(0, 0, Resolution, Resolution);
            context.Rasterizer.State = new RasterizerState(device, new RasterizerStateDescription()
            {
                CullMode = CullMode.None,
                FillMode = FillMode.Solid
            });

            int newOffset = 0;
            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                LayoutData.Offset = (uint)newOffset;
                LayoutData.Size = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;
                context.UpdateSubresource(ref LayoutData, LayoutConstantBuffer);
                return true;
            });

            context.PixelShader.SetShaderResource(1, null);
            context.PixelShader.SetShaderResource(2, null);
            context.PixelShader.SetShaderResource(3, null);

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);

            luminanceView.Dispose();
            chrominanceView.Dispose();
            Camera.ReleaseTexture();
        }
        
        private void UpdatePacking()
        {
            if (!Active)
                return;
            
            Meshes.UpdateMesh(Surfaces);

            var nextTexture = (CurrentTexture + 1) % 2;

            var oldTriangleCount = PreviousCount;
            var oldNumberOnSide = (int)Math.Ceiling(Math.Sqrt(oldTriangleCount / 2.0));

            var newTriangleCount = Meshes.TotalNumberOfTriangles;
            var newNumberOfSide = (int)Math.Ceiling(Math.Sqrt(newTriangleCount / 2.0));

            var device = Resources.D3DDevice;
            var context = Resources.D3DDeviceContext;

            var currentTextureSet = MeshTextures[CurrentTexture];
            var nextTextureSet = MeshTextures[nextTexture];

            context.VertexShader.Set(UpdateVertexShader);
            context.GeometryShader.Set(UpdateGeometryShader);
            context.GeometryShader.SetConstantBuffer(2, UpdateLayoutConstantBuffer);
            context.PixelShader.Set(UpdatePixelShader);
            context.PixelShader.SetShaderResource(0, currentTextureSet.ColorResourceView);
            context.PixelShader.SetShaderResource(1, currentTextureSet.QualityAndTimeResourceView);

            context.ClearRenderTargetView(nextTextureSet.RenderColorView, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            context.ClearRenderTargetView(nextTextureSet.RenderQualityAndTimeView, new RawColor4(0.0f, 0.0f, 0.0f, 0.0f));
            context.OutputMerger.SetRenderTargets(null, nextTextureSet.RenderColorView);

            context.Rasterizer.SetViewport(0.0f, 0.0f, Resolution, Resolution);

            int newOffset = 0;
            Meshes.Draw(numberOfIndices =>
            {
                context.DrawIndexed(numberOfIndices, 0, 0);
            },
            (guid, numberOfIndices) =>
            {
                UpdateLayoutData.OldSize = (uint)oldNumberOnSide;
                UpdateLayoutData.NewOffset = (uint)newOffset;
                UpdateLayoutData.NewSize = (uint)newNumberOfSide;
                newOffset += numberOfIndices / 3;

                if (!PreviousOffsets.ContainsKey(guid))
                    return false;

                UpdateLayoutData.OldOffset = (uint)PreviousOffsets[guid];

                context.UpdateSubresource(ref UpdateLayoutData, UpdateLayoutConstantBuffer);
                return true;
            });
            context.PixelShader.SetShaderResource(0, null);
            context.PixelShader.SetShaderResource(1, null);

            context.OutputMerger.SetRenderTargets(null, (RenderTargetView)null);

            CurrentTexture = nextTexture;
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
                var boundingBox = new SpatialBoundingBox()
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
            foreach (var meshTexture in MeshTextures)
            {
                meshTexture.Initialize();
            }
        }

        public async void CreateDeviceDependentResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            Meshes = new MeshCollection(Resources, await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Updating\VertexShader.cso")));
            Meshes.OnMeshChanged += RequestPackingUpdate;

            UpdateVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Updating\VertexShader.cso"));
            UpdateGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Updating\GeometryShader.cso"));
            UpdatePixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Updating\PixelShader.cso"));

            PrepassVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Projection\DepthPrepassVertexShader.cso"));
            PrepassPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Projection\DepthPrepassPixelShader.cso"));

            ProjectionVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Projection\VertexShader.cso"));
            ProjectionGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Projection\GeometryShader.cso"));
            ProjectionPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Projection\PixelShader.cso"));

            RenderVertexShader = ToDispose(await DirectXHelper.LoadShader<VertexShader>(device, folder, @"Content\Shaders\Mesh Rendering\VertexShader.cso"));
            RenderGeometryShader = ToDispose(await DirectXHelper.LoadShader<GeometryShader>(device, folder, @"Content\Shaders\Mesh Rendering\GeometryShader.cso"));
            RenderPixelShader = ToDispose(await DirectXHelper.LoadShader<PixelShader>(device, folder, @"Content\Shaders\Mesh Rendering\PixelShader.cso"));

            DepthTexture = ToDispose(new Texture2D(device, new Texture2DDescription()
            {
                Width = Camera.Width,
                Height = Camera.Height,
                ArraySize = 1,
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                Format = SharpDX.DXGI.Format.R32_Typeless,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 0,
                SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0)
            }));
            var depthTargetDescription = new DepthStencilViewDescription()
            {
                Format = SharpDX.DXGI.Format.D32_Float,
                Dimension = DepthStencilViewDimension.Texture2D,
                Flags = DepthStencilViewFlags.None
            };
            depthTargetDescription.Texture2D.MipSlice = 0;
            DepthTarget = ToDispose(new DepthStencilView(device, DepthTexture, depthTargetDescription));
            var depthResourceDescription = new ShaderResourceViewDescription()
            {
                Format = SharpDX.DXGI.Format.R32_Float,
                Dimension = ShaderResourceViewDimension.Texture2D
            };
            depthResourceDescription.Texture2D.MipLevels = -1;
            depthResourceDescription.Texture2D.MostDetailedMip = 0;
            DepthResource = ToDispose(new ShaderResourceView(device, DepthTexture, depthResourceDescription));

            UpdateLayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref UpdateLayoutData));
            LayoutConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref LayoutData));
            CameraConstantBuffer = ToDispose(SharpDX.Direct3D11.Buffer.Create(device, BindFlags.ConstantBuffer, ref CameraData));

            await InitializeSurfaceObservation();
            Active = true;
        }
    }
}
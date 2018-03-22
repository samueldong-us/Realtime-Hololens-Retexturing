// Copyright (C) 2018 The Regents of the University of California (Regents).
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
//
//     * Redistributions of source code must retain the above copyright
//       notice, this list of conditions and the following disclaimer.
//
//     * Redistributions in binary form must reproduce the above
//       copyright notice, this list of conditions and the following
//       disclaimer in the documentation and/or other materials provided
//       with the distribution.
//
//     * Neither the name of The Regents or University of California nor the
//       names of its contributors may be used to endorse or promote products
//       derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
//
// Please contact the author of this library if you have any questions.
// Author: Samuel Dong (samuel_dong@umail.ucsb.edu)
using Realtime_Hololens_Retexturing.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;
using Windows.Storage;

namespace Realtime_Hololens_Retexturing.Content
{
    internal class MainRenderer : Disposer
    {
        private const int Resolution = 4096;

        #region Flags

        private bool CameraPaused;
        private bool Debug;
        private bool ExportRequested;
        private bool GeometryPaused;
        private bool ProjectionRequested;
        private bool UpdateRequested;

        #endregion Flags

        private bool Active;
        private PhysicalCamera Camera;
        private SpatialCoordinateSystem CoordinateSystem;
        private MeshCollection Meshes;
        private MeshRenderer MeshRenderer;
        private MeshTexturer MeshTexturer;
        private int PreviousCount;
        private Dictionary<Guid, int> PreviousOffsets;
        private DeviceResources Resources;
        private SpeechRecognizer SpeechRecognizer;
        private SpatialSurfaceObserver SurfaceObserver;
        private Dictionary<Guid, SpatialSurfaceInfo> Surfaces;
        private TextureDebugRenderer TextureDebugger;

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
            MeshRenderer = new MeshRenderer(resources);

            Camera.FrameUpdated += RequestMeshProjection;

            SetupSpeechRecognition();
        }

        public async void CreateDeviceDependentResources()
        {
            var device = Resources.D3DDevice;
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;

            Meshes = new MeshCollection(Resources, await DirectXHelper.ReadDataAsync(await folder.GetFileAsync(@"Content\Shaders\Mesh Updating\VertexShader.cso")));
            Meshes.OnMeshChanged += RequestPackingUpdate;

            await MeshRenderer.CreateDeviceDependantResources();

            await ModelLoader.LoadObj(Resources, @"Content\Assets\cube rounded.obj");

            await InitializeSurfaceObservation();
            Active = true;
        }

        public void Initialize(SpatialCoordinateSystem coordinateSystem)
        {
            CoordinateSystem = coordinateSystem;
            CreateDeviceDependentResources();
        }

        public void Render()
        {
            if (!Active)
                return;

            MeshRenderer.RenderMesh(Meshes, MeshTexturer.ColorResourceView);
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

        public void UpdateTransform(SpatialCoordinateSystem coordinateSystem)
        {
            CoordinateSystem = coordinateSystem;
            Meshes.UpdateTransform(coordinateSystem);
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
    }
}
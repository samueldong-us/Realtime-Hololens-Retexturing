using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace Realistic_Hololens_Rendering.Common
{
    internal class MeshCollection
    {
        public delegate void OnMeshChangedHandler(Dictionary<Guid, int> oldOffsets, int oldCount, Dictionary<Guid, SpatialSurfaceInfo> surfaces);
        public event OnMeshChangedHandler OnMeshChanged;
        public int TotalNumberOfTriangles { get => Meshes.Sum(pair => pair.Value.NumberOfIndices) / 3; }
        private object MeshLock = new object();
        private SortedDictionary<Guid, SpatialMesh> Meshes;
        private DeviceResources Resources;
        private byte[] VertexShaderBytecode;

        public MeshCollection(DeviceResources resources, byte[] vertexShaderBytecode)
        {
            Resources = resources;
            Meshes = new SortedDictionary<Guid, SpatialMesh>();
            VertexShaderBytecode = vertexShaderBytecode;
            OnMeshChanged += (_, __, ___) => { };
        }

        public void Draw(Action<int> drawingFunction, Func<Guid, int, bool> preprocessingFunction = null)
        {
            lock (MeshLock)
            {
                foreach (var mesh in Meshes)
                {
                    if (preprocessingFunction?.Invoke(mesh.Key, mesh.Value.NumberOfIndices) ?? true)
                    {
                        mesh.Value.Draw(drawingFunction);
                    }
                }
            }
        }

        public void UpdateTransform(SpatialCoordinateSystem coordinateSystem)
        {
            lock (MeshLock)
            {
                foreach (var mesh in Meshes.Values)
                {
                    mesh.UpdateTransform(coordinateSystem);
                }
            }
        }

        public void ProcessSurfaces(IReadOnlyDictionary<Guid, SpatialSurfaceInfo> surfaces)
        {
            var oldOffsets = CalculateOffsets();
            var oldCount = TotalNumberOfTriangles;
            OnMeshChanged(oldOffsets, oldCount, surfaces.ToDictionary(pair => pair.Key, pair => pair.Value));
        }

        public void UpdateMesh(IReadOnlyDictionary<Guid, SpatialSurfaceInfo> surfaces)
        {
            lock (MeshLock)
            {
                foreach (var guid in surfaces.Keys)
                {
                    if (!Meshes.ContainsKey(guid))
                    {
                        Meshes[guid] = new SpatialMesh(Resources);
                        Meshes[guid].ProcessMeshData(surfaces[guid], VertexShaderBytecode);
                    }
                }

                var nonexistantGuids = Meshes.Keys.Except(surfaces.Keys).ToArray();
                foreach (var guid in nonexistantGuids)
                {
                    Meshes[guid].Dispose();
                    Meshes.Remove(guid);
                }
            }
        }

        private Dictionary<Guid, int> CalculateOffsets()
        {
            lock (MeshLock)
            {
                var offsets = new Dictionary<Guid, int>();
                int offset = 0;
                foreach (var mesh in Meshes)
                {
                    offsets[mesh.Key] = offset;
                    offset += mesh.Value.NumberOfIndices / 3;
                }
                return offsets;
            }
        }
    }
}
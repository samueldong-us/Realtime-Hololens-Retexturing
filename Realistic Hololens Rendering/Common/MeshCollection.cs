using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;

namespace Realistic_Hololens_Rendering.Common
{
    internal class MeshCollection
    {
        private object MeshLock = new object();
        private Dictionary<Guid, SpatialMesh> Meshes;
        private DeviceResources Resources;
        private byte[] VertexShaderBytecode;

        public MeshCollection(DeviceResources resources, byte[] vertexShaderBytecode)
        {
            Resources = resources;
            Meshes = new Dictionary<Guid, SpatialMesh>();
            VertexShaderBytecode = vertexShaderBytecode;
        }

        public void Draw(Action<int> drawingFunction)
        {
            lock (MeshLock)
            {
                foreach (var mesh in Meshes.Values)
                {
                    mesh.Draw(drawingFunction);
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
            lock (MeshLock)
            {
                foreach (var guid in surfaces.Keys)
                {
                    if (!Meshes.ContainsKey(guid))
                    {
                        Meshes[guid] = new SpatialMesh(Resources);
                    }
                    Meshes[guid].ProcessMeshData(surfaces[guid], VertexShaderBytecode);
                }

                var nonexistantGuids = Meshes.Keys.Except(surfaces.Keys).ToArray();
                foreach (var guid in nonexistantGuids)
                {
                    Meshes[guid].Dispose();
                    Meshes.Remove(guid);
                }
            }
        }
    }
}
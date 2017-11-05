using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Perception.Spatial;
using Windows.Perception.Spatial.Surfaces;
using Windows.Storage;

namespace Realistic_Hololens_Rendering.Common
{
    internal class MeshCollection
    {
        public delegate void OnMeshChangedHandler(Dictionary<Guid, int> oldOffsets, int oldCount, Dictionary<Guid, SpatialSurfaceInfo> surfaces);
        public event OnMeshChangedHandler OnMeshChanged;
        public int TotalNumberOfTriangles { get => Meshes.Sum(pair => pair.Value.NumberOfIndices) / 3; }

        private const float Resolution = 4096.0f;
        private const float Border = 1.0f;

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

        public string ExportMesh(string materialFile, string materialName)
        {
            ProcessMeshes(out var positions, out var normals, out var uvs, out var faces);
            var objBuilder = new StringBuilder();
            foreach (var position in positions)
            {
                objBuilder.AppendLine($"v {position.X} {position.Y} {position.Z}");
            }
            foreach (var normal in normals)
            {
                objBuilder.AppendLine($"vn {normal.X} {normal.Y} {normal.Z}");
            }
            foreach (var uv in uvs)
            {
                objBuilder.AppendLine($"vt {uv.X} {uv.Y}");
            }
            objBuilder
                .AppendLine($"mtllib {materialFile}")
                .AppendLine($"usemtl {materialName}");
            foreach (var face in faces)
            {
                var vertices = face.Vertices.Select(vertex => $"{vertex.PositionIndex + 1}/{vertex.UVIndex + 1}/{vertex.NormalIndex + 1}");
                objBuilder.AppendLine($"f {string.Join(" ", vertices)}");
            }
            return objBuilder.ToString();
        }

        private void ProcessMeshes(out List<Vector3> positions, out List<Vector3> normals, out List<Vector2> uvs, out List<Face> faces)
        {
            positions = new List<Vector3>();
            normals = new List<Vector3>();
            uvs = new List<Vector2>();
            faces = new List<Face>();

            var positionLookup = new Dictionary<Vector3, int>();
            var normalLookup = new Dictionary<Vector3, int>();
            var uvLookup = new Dictionary<Vector2, int>();

            int offset = 0;
            int size = (int)Math.Ceiling(Math.Sqrt(TotalNumberOfTriangles / 2.0)); ;
            lock (MeshLock)
            {
                foreach (var mesh in Meshes)
                {
                    var meshData = mesh.Value.Mesh;
                    if (meshData == null)
                        continue;
                    var positionArray = BytesTo(meshData.VertexPositions.Data.ToArray(), reader =>
                    {
                        var output = new Vector3()
                        {
                            X = Math.Max(reader.ReadInt16() / (float)Int16.MaxValue, -1.0f),
                            Y = Math.Max(reader.ReadInt16() / (float)Int16.MaxValue, -1.0f),
                            Z = Math.Max(reader.ReadInt16() / (float)Int16.MaxValue, -1.0f)
                        };
                        reader.ReadInt16();
                        return Vector3.Transform(output, Matrix4x4.Transpose(mesh.Value.TransformData.VertexTransform));
                    }).ToArray();
                    var normalArray = BytesTo(meshData.VertexNormals.Data.ToArray(), reader =>
                    {
                        var output = new Vector3()
                        {
                            X = Math.Max(reader.ReadSByte() / (float)SByte.MaxValue, -1.0f),
                            Y = Math.Max(reader.ReadSByte() / (float)SByte.MaxValue, -1.0f),
                            Z = Math.Max(reader.ReadSByte() / (float)SByte.MaxValue, -1.0f)
                        };
                        reader.ReadSByte();
                        return Vector3.Transform(output, Matrix4x4.Transpose(mesh.Value.TransformData.NormalTransform));
                    }).ToArray();
                    var indices = BytesTo(meshData.TriangleIndices.Data.ToArray(), reader =>
                    {
                        return reader.ReadUInt16();
                    }).ToArray();
                    for (int primitiveID = 0; primitiveID < indices.Length / 3; primitiveID++)
                    {
                        var face = new Face();
                        for (int vertexID = 0; vertexID < 3; vertexID++)
                        {
                            int index = indices[primitiveID * 3 + vertexID];
                            var uv = CalculateUV(primitiveID + offset, vertexID, size);
                            uv.Y = 1.0f - uv.Y;
                            face.Vertices[vertexID] = new Vertex()
                            {
                                PositionIndex = LookupOrAdd(positionArray[index], positionLookup, positions),
                                NormalIndex = LookupOrAdd(normalArray[index], normalLookup, normals),
                                UVIndex = LookupOrAdd(uv, uvLookup, uvs)
                            };
                        }
                        faces.Add(face);
                    }
                    offset += mesh.Value.NumberOfIndices / 3;
                }
            }
        }

        private int LookupOrAdd<T>(T value, IDictionary<T, int> lookup, IList<T> list)
        {
            if (!lookup.ContainsKey(value))
            {
                lookup[value] = list.Count;
                list.Add(value);
            }
            return lookup[value];
        }

        private Vector2 CalculateUV(int primitiveID, int vertexID, int size)
        {
            float pixel = 1.0f / Resolution * size;
            Vector2[] Offsets =
            {
                new Vector2(Border * pixel, Border * pixel),
                new Vector2(1.0f - 2.0f * Border * pixel, Border * pixel),
                new Vector2(Border * pixel, 1.0f - 2.0f * Border * pixel),
                new Vector2(1.0f - Border * pixel, 2.0f * Border * pixel),
                new Vector2(1.0f - Border * pixel, 1.0f - Border * pixel),
                new Vector2(2.0f * Border * pixel, 1.0f - Border * pixel)
            };
            int squareID = primitiveID / 2;
            float squareSize = 1.0f / size;
            Vector2 topLeft;
            topLeft.X = (squareID % size) * squareSize;
            topLeft.Y = (squareID / size) * squareSize;
            return topLeft + Offsets[primitiveID % 2 * 3 + vertexID] * squareSize;
        }

        private IEnumerable<T> BytesTo<T>(byte[] data, Func<BinaryReader, T> processor)
        {
            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    yield return processor(reader);
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
using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace MarcosPereira.PolygonReducer {
    // This class is a ScriptableObject so that shared references to its objects
    // are preserved between serializations made by Unity.
    public class ExtendedMesh : ScriptableObject {
        // Original mesh. Points to a mesh asset, so do not modify it directly!
        // Do not read vertices or triangles through it either - use
        // this.vertices or this.triangles instead as that is more efficient.
        public Mesh originalMesh;

        // Cached vertices of original mesh.
        public Vector3[] vertices;

        // Contains indices of vertices that have been deleted.
        public SerializableHashSet<int> deletedVertices;

        // Holds mesh triangles, including all submeshes.
        // Vertex indices will be modified.
        public int[] triangles;

        // Contains indices of triangles that have been deleted.
        public SerializableHashSet<int> deletedTriangles;

        // Holds triangle normals, which have nothing to do with vertex normals.
        public SerializableDictionary<int, Vector3> triangleNormals;

        // For each vertex, holds the indices of vertices it is connected to
        // by some triangle.
        public SerializableHashSet<int>[] neighborVertices;

        // For each vertex, holds the indices of triangles it is a part of.
        public SerializableHashSet<int>[] adjacentTriangles;

        // Holds the indices of vertices that should not be moved to prevent
        // gaps on mesh surface.
        public SerializableHashSet<int> seams;

        // A collapser object, in charge of reducing polygons of this mesh.
        [SerializeField]
        private Collapser collapser;

        // Factory method used in place of constructor as this is a
        // ScriptableObject.
        public static ExtendedMesh Create(Mesh mesh) {
            ExtendedMesh m =
                ScriptableObject.CreateInstance<ExtendedMesh>();

            m.originalMesh = mesh;
            m.vertices = mesh.vertices;
            m.deletedVertices = new SerializableHashSet<int>();

            // It is not explicitly stated that mesh.triangles contains each
            // submesh's triangles in the correct order, so we fetch them
            // separately.

            var triangles = new List<int>();

            for (int i = 0; i < mesh.subMeshCount; i++) {
                triangles.AddRange(mesh.GetTriangles(i));
            }

            m.triangles = triangles.ToArray();
            m.deletedTriangles = new SerializableHashSet<int>();
            m.triangleNormals = m.GetTriangleNormals();

            (m.neighborVertices, m.adjacentTriangles) = m.GetNeighbors();

            m.seams = Seams.GetSeams(m);

            m.collapser = new Collapser(m, Cost.Default);

            return m;
        }

        public Mesh GetMesh(float reductionPercent) {
            if (reductionPercent < 0 || reductionPercent > 100f) {
                Debug.LogWarning(
                    $"Reduction percentage of {reductionPercent} is out of " +
                    "bounds. Clamping to [0, 100]."
                );
            }

            float reductionFactor = Mathf.Clamp01(reductionPercent / 100f);

            this.collapser.SetQuality(reductionFactor);
            return this.GetMesh();
        }

        // Recalculates a vertex's neighbor vertices based on its adjacent
        // triangles.
        // I tried just updating based on heuristics instead of fully
        // recalculating, but it didn't work and may require a lot of additional
        // computation.
        public void RecalculateNeighborVertices(int vertex) {
            this.neighborVertices[vertex] = new SerializableHashSet<int>();

            foreach (int t in this.adjacentTriangles[vertex]) {
                for (int i = 0; i < 3; i++) {
                    int x = this.triangles[t + i];

                    if (x != vertex) {
                        _ = this.neighborVertices[vertex].Add(x);
                    }
                }
            }
        }

        public void ReplaceVertex(int triangle, int from, int to) {
            for (int i = 0; i < 3; i++) {
                int v = this.triangles[triangle + i];

                if (v == from) {
                    this.triangles[triangle + i] = to;
                    break;
                }
            }
        }

        public bool TriangleHasVertex(int t, int v) =>
            this.triangles[t] == v ||
            this.triangles[t + 1] == v ||
            this.triangles[t + 2] == v;

        public Vector3 GetTriangleNormal(int i) {
            int u = this.triangles[i];
            int v = this.triangles[i + 1];
            int w = this.triangles[i + 2];

            Vector3 a = this.vertices[v] - this.vertices[u];
            Vector3 b = this.vertices[w] - this.vertices[u];

            return Vector3.Cross(a, b).normalized;
        }

        private Mesh GetMesh() {
            int newVertexCount =
                this.vertices.Length - this.deletedVertices.Count;

            var newVertices = new Vector3[newVertexCount];

            // Normals
            //
            // We keep original normals for all vertices.
            // Recalculating normals was decided against since at low detail
            // the angles get quite sharp.
            //
            // /!\ Attention: If updating this code to recalculate normals,
            // make sure original normals are still kept for seam vertices
            // (this.seams). That prevents visual artifacts in the border of
            // two visually connected meshes, such as terrain chunks.
            Vector3[] originalNormals = this.originalMesh.normals;
            Vector3[] newNormals = null;

            if (originalNormals != null && originalNormals.Length > 0) {
                newNormals = new Vector3[newVertexCount];
            }

            // UVs

            var originalUVs = new Vector2[][] {
                this.originalMesh.uv,
                this.originalMesh.uv2,
                this.originalMesh.uv3,
                this.originalMesh.uv4,
                this.originalMesh.uv5,
                this.originalMesh.uv6,
                this.originalMesh.uv7,
                this.originalMesh.uv8
            };

            var newUVs = new Vector2[originalUVs.Length][];

            for (int i = 0; i < originalUVs.Length; i++) {
                Vector2[] array = originalUVs[i];

                if (array != null && array.Length > 0) {
                    newUVs[i] = new Vector2[newVertexCount];
                }
            }

            // Colors

            Color32[] originalColors32 = this.originalMesh.colors32;
            Color32[] newColors32 = null;

            if (originalColors32 != null && originalColors32.Length > 0) {
                newColors32 = new Color32[newVertexCount];
            }

            // Tangents

            Vector4[] originalTangents = this.originalMesh.tangents;
            Vector4[] newTangents = null;

            if (originalTangents != null && originalTangents.Length > 0) {
                newTangents = new Vector4[newVertexCount];
            }

            // Bone weights

            NativeArray<BoneWeight1> originalBoneWeights =
                this.originalMesh.GetAllBoneWeights();
            // Bones per vertex array size will be either zero or equal
            // to vertex count.
            NativeArray<byte> originalBonesPerVertex =
                this.originalMesh.GetBonesPerVertex();
            List<BoneWeight1> newBoneWeights = null;
            byte[] newBonesPerVertex = null;

            if (originalBonesPerVertex.Length > 0) {
                // Initial bone list size is a good guess at new number of bones
                // (4 per vertex)
                newBoneWeights = new List<BoneWeight1>(newVertexCount * 4);

                newBonesPerVertex = new byte[newVertexCount];
            }

            // Copy original vertex data into new mesh, for undeleted vertices.
            var newIndices = new Dictionary<int, int>();
            int j = 0;
            int boneCounter = 0;
            for (int i = 0; i < this.vertices.Length; i++) {
                if (!this.deletedVertices.Contains(i)) {
                    newVertices[j] = this.vertices[i];

                    // Copy normals
                    if (newNormals != null) {
                        newNormals[j] = originalNormals[i];
                    }

                    // Copy UVs
                    for (int k = 0; k < newUVs.Length; k++) {
                        if (newUVs[k] != null) {
                            newUVs[k][j] = originalUVs[k][i];
                        }
                    }

                    // Copy vertex colors
                    if (newColors32 != null) {
                        newColors32[j] = originalColors32[i];
                    }

                    // Copy tangents
                    if (newTangents != null) {
                        newTangents[j] = originalTangents[i];
                    }

                    // Copy bone weights
                    if (newBonesPerVertex != null) {
                        newBonesPerVertex[j] = originalBonesPerVertex[i];

                        for (int k = 0; k < originalBonesPerVertex[i]; k++) {
                            newBoneWeights
                                .Add(originalBoneWeights[boneCounter]);

                            boneCounter++;
                        }
                    }

                    newIndices.Add(i, j);
                    j++;
                } else {
                    // If this vertex has been deleted, make sure bone counter
                    // is incremented.
                    if (newBonesPerVertex != null) {
                        boneCounter += originalBonesPerVertex[i];
                    }
                }
            }

            // We don't apply baseVertex offset when setting submeshes, so
            // we may have to have a UInt32 index format even if original was
            // UInt16.
            IndexFormat newIndexFormat =
                UnityEngine.Rendering.IndexFormat.UInt16;

            if (newVertexCount > 65535) {
                newIndexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            // Create new mesh
            // Assigning triangles automatically recalculates mesh bounds.
            var mesh = new Mesh() {
                name = $"{this.originalMesh.name} (reduced)",
                indexFormat = newIndexFormat,
                vertices = newVertices,
                normals = newNormals,
                uv = newUVs[0],
                uv2 = newUVs[1],
                uv3 = newUVs[2],
                uv4 = newUVs[3],
                uv5 = newUVs[4],
                uv6 = newUVs[5],
                uv7 = newUVs[6],
                uv8 = newUVs[7],
                colors32 = newColors32,
                tangents = newTangents,
                bindposes = this.originalMesh.bindposes,
                subMeshCount = this.originalMesh.subMeshCount
            };

            this.AssignTriangles(mesh, newIndices);

            // Assign new bones
            if (newBonesPerVertex != null) {
                mesh.SetBoneWeights(
                    new NativeArray<byte>(
                        newBonesPerVertex,
                        Allocator.Temp
                    ),
                    new NativeArray<BoneWeight1>(
                        newBoneWeights.ToArray(),
                        Allocator.Temp
                    )
                );
            }

            mesh.Optimize();

            return mesh;
        }

        private void AssignTriangles(
            Mesh mesh,
            Dictionary<int, int> newIndices
        ) {
            SubMeshDescriptor GetSubmesh(int i) =>
                this.originalMesh.GetSubMesh(i);

            // Copy triangles using new indices

            var newTriangles = new List<int>[this.originalMesh.subMeshCount];
            newTriangles[0] = new List<int>();

            int submesh = 0;
            int indexCount = GetSubmesh(0).indexCount;

            for (int i = 0; i < this.triangles.Length; i += 3) {
                if (!this.deletedTriangles.Contains(i)) {
                    // Move on to next submesh if reached current one's index
                    // count
                    if (i >= indexCount) {
                        submesh++;
                        indexCount += GetSubmesh(submesh).indexCount;
                        newTriangles[submesh] = new List<int>();

                        if (submesh >= this.originalMesh.subMeshCount) {
                            throw new System.Exception(
                                "ExtendedMesh: Submesh index is larger than " +
                                "subMeshCount."
                            );
                        }
                    }

                    for (int k = 0; k < 3; k++) {
                        newTriangles[submesh]
                            .Add(newIndices[this.triangles[i + k]]);
                    }
                }
            }

            for (int k = 0; k < this.originalMesh.subMeshCount; k++) {
                mesh.SetTriangles(newTriangles[k], k);
            }
        }

        private (SerializableHashSet<int>[], SerializableHashSet<int>[]) GetNeighbors() {
            int vertexCount = this.vertices.Length;
            var neighborVertices = new SerializableHashSet<int>[vertexCount];
            var adjacentTriangles = new SerializableHashSet<int>[vertexCount];

            int triangleCount = this.triangles.Length;
            for (int i = 0; i < triangleCount; i += 3) {
                for (int j = 0; j < 3; j++) {
                    int v = this.triangles[i + j];

                    // Store the two neighbor vertices of each vertex in the
                    // triangle

                    if (neighborVertices[v] == null) {
                        neighborVertices[v] = new SerializableHashSet<int>();
                    }

                    for (int k = 0; k < 3; k++) {
                        if (j == k) {
                            continue;
                        }

                        _ = neighborVertices[v].Add(this.triangles[i + k]);
                    }

                    // Add triangle to current vertex's adjacent triangles

                    if (adjacentTriangles[v] == null) {
                        adjacentTriangles[v] =
                            new SerializableHashSet<int>();
                    }

                    _ = adjacentTriangles[v].Add(i);
                }
            }

            return (neighborVertices, adjacentTriangles);
        }

        // Triangle normals are stored in a dictionary because triangle indices
        // increase in steps of 3. Even though we know the required length from
        // the start, we don't make it an array because then it would need to
        // be accessed with triangleNormals[t / 3] where t is the triangle
        // index. This is easily forgotten.
        private SerializableDictionary<int, Vector3> GetTriangleNormals() {
            var normals = new SerializableDictionary<int, Vector3>();

            for (int i = 0; i < this.triangles.Length; i += 3) {
                normals[i] = this.GetTriangleNormal(i);
            }

            return normals;
        }
    }
}

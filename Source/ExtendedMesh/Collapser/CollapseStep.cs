using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    /// <summary>
    /// We store vertex collapse information in a CollapseStep object before
    /// applying it to an ExtendedMesh. This makes it easier to undo the collapse
    /// later.
    /// </summary>
    public class CollapseStep {
        public readonly int fromVertex;
        public readonly int toVertex;
        public HashSet<int> triangleDeletions = new HashSet<int>();

        // Store previous and new normal vector so we can roll back if we want
        // to undo this step.
        public Dictionary<int, (Vector3 from, Vector3 to)>
            triangleNormalUpdates = new Dictionary<int, (Vector3, Vector3)>();

        // Store indices of triangles which had fromVertex replaced with
        // toVertex
        public List<int> vertexReplacements =
            new List<int>();

        private readonly ExtendedMesh extendedMesh;

        private readonly Dictionary<int, HashSet<int>>
            adjacentTriangleRemovals = new Dictionary<int, HashSet<int>>();

        private readonly Dictionary<int, HashSet<int>>
            adjacentTriangleAdditions = new Dictionary<int, HashSet<int>>();

        public CollapseStep(ExtendedMesh m, int fromVertex, int toVertex) {
            this.extendedMesh = m;
            this.fromVertex = fromVertex;
            this.toVertex = toVertex;

            int u = fromVertex;
            int v = toVertex;

            if (v == -1) {
                // u is a vertex all by itself, so we will just delete it
                return;
            }

            // For each triangle that vertex u is a part of
            foreach (int t in m.adjacentTriangles[u]) {
                // Delete triangles that contain both u and v
                if (m.TriangleHasVertex(t, v)) {
                    _ = this.triangleDeletions.Add(t);

                    // Remove deleted triangle from its vertices' adjacent
                    // triangles
                    for (int i = 0; i < 3; i++) {
                        int vertex = m.triangles[t + i];

                        if (vertex == u) {
                            // Do not change u's adjacent triangles, which would
                            // cause an error as we are iterating over them.
                            continue;
                        }

                        this.AdjacentTriangleRemoval(vertex, t);
                    }

                    // Skip to next triangle
                    continue;
                }

                // Update remaining triangles to have v instead of u
                this.vertexReplacements.Add(t);

                // Add this triangle to v's sides
                // (we know it wasn't there before because triangles with both
                // u and v are deleted)
                this.AdjacentTriangleAddition(v, t);

                // Update triangle normals
                this.triangleNormalUpdates.Add(
                    t,
                    (m.triangleNormals[t], m.GetTriangleNormal(t))
                );
            }
        }

        public void Apply() {
            if (this.toVertex == -1) {
                // fromVertex is a vertex all by itself, so we just delete it
                _ = this.extendedMesh.deletedVertices.Add(this.fromVertex);
                return;
            }

            // Triangle deletions
            foreach (int triangle in this.triangleDeletions) {
                _ = this.extendedMesh.deletedTriangles.Add(triangle);
            }

            // Adjacent triangle removals
            foreach (int vertex in this.adjacentTriangleRemovals.Keys) {
                HashSet<int> removals = this.adjacentTriangleRemovals[vertex];

                foreach (int triangle in removals) {
                    _ = this.extendedMesh.adjacentTriangles[vertex]
                        .Remove(triangle);
                }
            }

            // Adjacent triangle additions
            foreach (int vertex in this.adjacentTriangleAdditions.Keys) {
                HashSet<int> additions = this.adjacentTriangleAdditions[vertex];

                foreach (int triangle in additions) {
                    _ = this.extendedMesh.adjacentTriangles[vertex]
                        .Add(triangle);
                }
            }

            // Vertex replacements
            foreach (int triangle in this.vertexReplacements) {
                this.extendedMesh.ReplaceVertex(
                    triangle,
                    this.fromVertex,
                    this.toVertex
                );
            }

            // Triangle normals
            foreach (
                KeyValuePair<int, (Vector3 from, Vector3 to)> x in
                this.triangleNormalUpdates
            ) {
                this.extendedMesh.triangleNormals[x.Key] = x.Value.to;
            }

            _ = this.extendedMesh.deletedVertices.Add(this.fromVertex);

            // Recalculate neighbor vertices based on newly updated adjacent
            // triangle lists
            this.RecalculateNeighborVertices();
        }

        /// <summary>Same as Apply(), but in reverse.</summary>
        public void Undo() {
            _ = this.extendedMesh.deletedVertices.Remove(this.fromVertex);

            if (this.toVertex == -1) {
                // fromVertex was a vertex all by itself, so we just readd it
                return;
            }

            // Triangle normals
            foreach (
                KeyValuePair<int, (Vector3 from, Vector3 to)> x in
                this.triangleNormalUpdates
            ) {
                this.extendedMesh.triangleNormals[x.Key] = x.Value.from;
            }

            // Vertex replacements
            foreach (int triangle in this.vertexReplacements) {
                this.extendedMesh.ReplaceVertex(
                    triangle,
                    this.toVertex,
                    this.fromVertex
                );
            }

            // Adjacent triangle additions
            foreach (int vertex in this.adjacentTriangleAdditions.Keys) {
                HashSet<int> additions = this.adjacentTriangleAdditions[vertex];

                foreach (int triangle in additions) {
                    _ = this.extendedMesh.adjacentTriangles[vertex]
                        .Remove(triangle);
                }
            }

            // Adjacent triangle removals
            foreach (int vertex in this.adjacentTriangleRemovals.Keys) {
                HashSet<int> removals = this.adjacentTriangleRemovals[vertex];

                foreach (int triangle in removals) {
                    _ = this.extendedMesh.adjacentTriangles[vertex]
                        .Add(triangle);
                }
            }

            // Triangle deletions
            foreach (int triangle in this.triangleDeletions) {
                _ = this.extendedMesh.deletedTriangles.Remove(triangle);
            }

            this.RecalculateNeighborVertices();
        }

        private void AdjacentTriangleAddition(int vertex, int triangle) {
            HashSet<int> set =
                this.GetOrCreateSet(this.adjacentTriangleAdditions, vertex);

            _ = set.Add(triangle);
        }

        private void AdjacentTriangleRemoval(int vertex, int triangle) {
            HashSet<int> set =
                this.GetOrCreateSet(this.adjacentTriangleRemovals, vertex);

            _ = set.Add(triangle);
        }

        private HashSet<int> GetOrCreateSet(
            Dictionary<int, HashSet<int>> d,
            int key
        ) {
            if (!d.TryGetValue(key, out HashSet<int> set)
            ) {
                set = new HashSet<int>();
                d.Add(key, set);
            }

            return set;
        }

        private void RecalculateNeighborVertices() {
            int u = this.fromVertex;
            int v = this.toVertex;

            // Recalculate v's neighbor vertices
            this.extendedMesh.RecalculateNeighborVertices(v);

            // Recalculate neighbor vertices of u's and v's neighbors

            var neighbors = new HashSet<int>(
                this.extendedMesh.neighborVertices[u]
            );

            neighbors.UnionWith(
                this.extendedMesh.neighborVertices[v]
            );

            foreach (int n in neighbors) {
                this.extendedMesh.RecalculateNeighborVertices(n);
            }
        }
    }
}

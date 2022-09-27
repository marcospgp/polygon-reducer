using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.PolygonReducer {
    public static class Seams {
        // Get seam vertices of a given mesh. These should not be manipulated to
        // avoid creating holes in the mesh.
        public static SerializableHashSet<int> GetSeams(ExtendedMesh m) {
            var seamVertices = new SerializableHashSet<int>();

            // Allocate a working list here to avoid creating a new one for each
            // vertex (which would produce a lot of garbage).
            var reusableSet = new HashSet<int>();

            for (int i = 0; i < m.vertices.Length; i++) {
                if (Seams.IsSeam(i, m, reusableSet)) {
                    _ = seamVertices.Add(i);
                }
            }

            return seamVertices;
        }

        private static bool IsSeam(
            int v,
            ExtendedMesh m,
            HashSet<int> reusableSet
        ) {
            // A vertex is not part of a seam if:
            //   * It has at least 3 adjacent triangles (pyramid is the simplest
            //     possible closed mesh), and
            //   * Its neighbor vertices form a closed loop consisting of at
            //     least 3 triangles.
            //
            // We iterate over the vertex's adjacent triangles and connect them
            // together trying to close a loop.

            SerializableHashSet<int> vTriangles = m.adjacentTriangles[v];

            if (vTriangles.Count < 3) {
                return true;
            }

            var pairs = new Dictionary<int, (int a, int b)>();

            // Store all vertex pairs from this vertex's triangles
            foreach (int t in vTriangles) {
                (int, int)? pair = Seams.GetVertexPair(v, t, m);

                if (pair.HasValue) {
                    pairs.Add(t, pair.Value);
                }
            }

            // * Try to match each triangle with each of the other
            //   triangles;
            // * If there is a match:
            // * Check if it closes the loop, and if it does, stop;
            // * Otherwise, update the current loop's ends, then start over
            //   comparing to the first triangle trying to close the loop;

            HashSet<int> trianglesInLoop = reusableSet;

            foreach (int triangle in pairs.Keys) {
                trianglesInLoop.Clear();
                _ = trianglesInLoop.Add(triangle);

                (int a, int b) loop = pairs[triangle];

                bool loopClosed =
                    Seams.TryToCloseLoop(loop, trianglesInLoop, pairs);

                if (loopClosed && trianglesInLoop.Count >= 3) {
                    // Vertex is not part of a mesh seam
                    return false;
                }

                // If loop wasn't closed with 3 or more triangles, keep trying
            }

            // Could not close a loop around vertex of at least 3 triangles, it
            // must be part of a mesh seam.
            return true;
        }

        /// <summary>
        /// Get the two vertices in a triangle other than the given one.
        /// We have to be careful when getting the vertices so that
        /// the vertex matching is consistent with face direction.
        /// We choose to store them in clockwise order. Anticlockwise
        /// would work the same, it only has to be consistent.
        /// </summary>
        /// <param name="v">Vertex index.</param>
        /// <param name="t">Triangle index.</param>
        /// <param name="m">Mesh to check.</param>
        private static (int a, int b)? GetVertexPair(
            int v,
            int t,
            ExtendedMesh m
        ) {
            if (m.triangles[t] == v) {
                return (m.triangles[t + 1], m.triangles[t + 2]);
            }

            if (m.triangles[t + 1] == v) {
                return (m.triangles[t + 2], m.triangles[t]);
            }

            if (m.triangles[t + 2] == v) {
                return (m.triangles[t], m.triangles[t + 1]);
            }

            Debug.LogError("Vertex v is not in triangle.");
            return null;
        }

        // This method is recursive, hence the exotic parameter choice.
        private static bool TryToCloseLoop(
            (int a, int b) loop,
            HashSet<int> trianglesInLoop,
            Dictionary<int, (int a, int b)> pairs
        ) {
            foreach (int t in pairs.Keys) {
                if (trianglesInLoop.Contains(t)) {
                    continue;
                }

                (int a, int b) pair = pairs[t];

                bool aMatch = loop.a == pair.b;
                bool bMatch = loop.b == pair.a;
                bool loopClosed = aMatch && bMatch;

                if (!aMatch && !bMatch) {
                    continue;
                }

                _ = trianglesInLoop.Add(t);

                // Check loop is closed after adding current triangle to loop,
                // so that count is correct.
                if (loopClosed) {
                    return true;
                }

                if (aMatch) {
                    loop = (pair.a, loop.b);
                } else if (bMatch) {
                    loop = (loop.a, pair.b);
                }

                return Seams.TryToCloseLoop(loop, trianglesInLoop, pairs);
            }

            // Could not close loop.
            return false;
        }
    }
}

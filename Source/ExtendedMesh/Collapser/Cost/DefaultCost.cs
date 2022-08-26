using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.PolygonReducer {
    public static class DefaultCost : ICost {
        // Default algorithm to calculate cost of collapsing a vertex into another.
        // Assigns a lower cost to redundant vertices,
        public static float Get(int from, int to, ExtendedMesh m) {
            Vector3 u = m.vertices[from];
            Vector3 v = m.vertices[to];

            SerializableHashSet<int> uTriangles = m.adjacentTriangles[from];
            int[] triangles = m.triangles;

            float edgeLength = (u - v).magnitude;

            // Get triangles containing vertices u and v
            var uvTriangles = new List<int>();

            foreach (int t in uTriangles) {
                if (
                    triangles[t] == to ||
                    triangles[t + 1] == to ||
                    triangles[t + 2] == to
                ) {
                    uvTriangles.Add(t);
                }
            }

            // Calculate curvature term

            float curvature = 0f;

            foreach (int t in uTriangles) {
                float minCurvature = 1f;

                Vector3 normal1 = m.triangleNormals[t];

                for (int j = 0; j < uvTriangles.Count; j++) {
                    int t2 = uvTriangles[j];

                    if (t == t2) {
                        // Do not compare a triangle with itself
                        continue;
                    }

                    Vector3 normal2 = m.triangleNormals[t2];

                    float dotProduct = Vector3.Dot(normal1, normal2);

                    minCurvature = Mathf.Min(
                        minCurvature,
                        (1f - dotProduct) / 2f
                    );
                }

                curvature = Mathf.Max(curvature, minCurvature);
            }

            return edgeLength * curvature;
        }
    }
}

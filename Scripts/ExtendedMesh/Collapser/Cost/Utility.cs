using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.PolygonReducer.CostFunctions {
    public static class Utility {
        /// <summary>
        /// <para>
        /// Calculate curvature term in [0, 1] (1 = sharpest).
        /// </para>
        /// <para>
        /// For each uTriangle, find the most coplanar uvTriangle.
        /// Of these pairs, the least coplanar determines the curvature.
        /// In other words, curvature is determined by the sharpest angle
        /// between a triangle containing u and a triangle containing u and v.
        /// </para>
        /// </summary>
        public static float Curvature(int u, int v, ExtendedMesh m) {
            SerializableHashSet<int> uTriangles = m.adjacentTriangles[u];
            int[] triangles = m.triangles;

            // Get triangles containing vertices u and v
            var uvTriangles = new List<int>();

            foreach (int t in uTriangles) {
                if (
                    triangles[t] == v ||
                    triangles[t + 1] == v ||
                    triangles[t + 2] == v
                ) {
                    uvTriangles.Add(t);
                }
            }

            float maxCurvature = 0f;

            foreach (int t in uTriangles) {
                float minCurvature = 1f;

                Vector3 normal1 = m.triangleNormals[t];

                // Ignore triangles containing v, as those will have curvature
                // of 0 because they are coplanar with themselves.
                // It would be fine to not ignore them, as we will pick max
                // curvature in the end.
                if (
                    triangles[t] == v ||
                    triangles[t + 1] == v ||
                    triangles[t + 2] == v
                ) {
                    continue;
                }

                for (int j = 0; j < uvTriangles.Count; j++) {
                    int t2 = uvTriangles[j];

                    Vector3 normal2 = m.triangleNormals[t2];

                    // Dot product = alignment in [-1, 1]
                    float dot = Vector3.Dot(normal1, normal2);

                    // Curvature = misalignment in [0, 1]
                    float curvature = (-dot + 1f) / 2f;

                    minCurvature = Mathf.Min(curvature, minCurvature);
                }

                maxCurvature = Mathf.Max(maxCurvature, minCurvature);
            }

            return maxCurvature;
        }
    }
}

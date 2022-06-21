using System;
using System.Collections.Generic;
using UnityEngine;
using MarcosPereira.UnityUtilities;

namespace MarcosPereira {
    [Serializable]
    public class Collapser {
        [SerializeField]
        private ExtendedMesh m;

        [SerializeField]
        private Costs costs;

        [SerializeField]
        private List<CollapseStep> collapseSteps = new List<CollapseStep>();

        [SerializeField]
        private int lastAppliedCollapseStep = -1;

        public Collapser(ExtendedMesh m) {
            this.m = m;

            // Calculate initial collapse costs.
            this.costs = this.GetInitialCollapseCosts();
        }

        /// <summary>
        /// Collapse or undo collapsing of vertices until target vertex
        /// reduction percentage is reached.
        /// </summary>
        /// <param name="reductionFactor"></param>
        public void SetQuality(float reductionFactor) {
            int vertexCount = this.m.vertices.Length;

            int currentVertexCount = vertexCount - this.m.deletedVertices.count;
            int targetVertexCount = this.GetTargetVertexCount(reductionFactor);

            if (currentVertexCount > targetVertexCount) {
                // Create new vertex collapse steps, or reapply previously
                // calculated ones if available, until target vertex count is
                // reached.
                while (
                    vertexCount - this.m.deletedVertices.count >
                    targetVertexCount
                ) {
                    if (
                        this.lastAppliedCollapseStep ==
                            this.collapseSteps.Count - 1
                    ) {
                        this.ApplyNewCollapseStep();
                    } else {
                        this.RedoCollapseStep();
                    }
                }
            } else if (currentVertexCount < targetVertexCount) {
                // Undo vertex collapses until target vertex count is reached or
                // we have run out of collapses to undo.
                while (
                    this.lastAppliedCollapseStep > -1 &&
                    vertexCount - this.m.deletedVertices.count <
                    targetVertexCount
                ) {
                    this.UndoCollapseStep();
                }
            }
        }

        private int GetTargetVertexCount(float reductionFactor) {
            int vertexCount = this.m.vertices.Length;
            int untouchableCount = this.m.seams.count;
            int touchableCount = vertexCount - untouchableCount;
            float keepFactor = 1f - reductionFactor;

            int targetVertexCount =
                untouchableCount +
                Mathf.FloorToInt(touchableCount * keepFactor);

            return targetVertexCount;
        }

        private void RedoCollapseStep() {
            this.collapseSteps[this.lastAppliedCollapseStep + 1].Apply();
            this.lastAppliedCollapseStep++;
        }

        private void UndoCollapseStep() {
            this.collapseSteps[this.lastAppliedCollapseStep].Undo();
            this.lastAppliedCollapseStep--;
        }

        private void ApplyNewCollapseStep() {
            Edge minCostEdge = this.costs.PopMinimumCost();

            var step = new CollapseStep(
                this.m,
                minCostEdge.fromVertex,
                minCostEdge.toVertex
            );

            this.collapseSteps.Add(step);

            // Store local copy of fromVertex's neighbors, in case applying this
            // new step messes with that list.
            var neighbors =
                new HashSet<int>(this.m.neighborVertices[step.fromVertex]);

            step.Apply();
            this.lastAppliedCollapseStep++;

            // Recompute edge collapse costs in fromVertex's neighborhood
            foreach (int n in neighbors) {
                if (this.m.seams.Contains(n)) {
                    continue;
                }

                this.UpdateCost(n);
            }
        }

        private Costs GetInitialCollapseCosts() {
            var costs = new Costs();

            // Seams are untouchable - we don't want to open holes in the mesh.
            SerializableHashSet<int> untouchableVertices = this.m.seams;

            int vertexCount = this.m.vertices.Length;
            for (int u = 0; u < vertexCount; u++) {
                if (untouchableVertices.Contains(u)) {
                    continue;
                }

                // Ensure this vertex has not been deleted. This ExtendedMesh
                // may have been manipulated before.
                if (this.m.deletedVertices.Contains(u)) {
                    continue;
                }

                (float cost, int targetVertex) =
                    this.GetCostAtVertex(u);

                costs.Add(new Edge(cost, u, targetVertex));
            }

            return costs;
        }

        private (float, int) GetCostAtVertex(int u) {
            SerializableHashSet<int> uTriangles = this.m.adjacentTriangles[u];
            int[] triangles = this.m.triangles;

            // u is a vertex all by itself, signal for deletion
            if (uTriangles.count == 0) {
                return (-1f, -1);
            }

            float minCost = float.MaxValue;
            int collapseTarget = -1;

            // Target vertices to which cost has been calculated
            var seen = new HashSet<int>();

            foreach (int t in uTriangles) {
                int[] triangle = new int[] {
                    triangles[t],
                    triangles[t + 1],
                    triangles[t + 2]
                };

                for (int i = 0; i < 3; i++) {
                    int v = triangles[t + i];

                    if (v == u) {
                        continue;
                    }

                    if (seen.Contains(v)) {
                        continue;
                    }

                    _ = seen.Add(v);

                    float cost = this.Cost(u, v);

                    if (cost < minCost) {
                        minCost = cost;
                        collapseTarget = v;
                    }
                }
            }

            // Ensure cost has been set above, otherwise could lead to issues.
            if (minCost == float.MaxValue) {
                throw new Exception(
                    "Could not determine collapse cost."
                );
            }

            return (minCost, collapseTarget);
        }

        private void UpdateCost(int u) {
            this.costs.RemoveByVertex(u);

            (float newCost, int v) = this.GetCostAtVertex(u);

            this.costs.Add(new Edge(newCost, u, v));
        }

        // Cost of collapsing vertex u into vertex v
        private float Cost(int u, int v) {
            Vector3 uPos = this.m.vertices[u];
            Vector3 vPos = this.m.vertices[v];

            SerializableHashSet<int> uTriangles = this.m.adjacentTriangles[u];
            int[] triangles = this.m.triangles;

            float edgeLength = (vPos - uPos).magnitude;

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

            // Calculate curvature term

            float curvature = 0f;

            foreach (int t in uTriangles) {
                float minCurvature = 1f;

                Vector3 normal1 = this.m.triangleNormals[t];

                for (int j = 0; j < uvTriangles.Count; j++) {
                    int t2 = uvTriangles[j];

                    if (t == t2) {
                        // Do not compare a triangle with itself
                        continue;
                    }

                    Vector3 normal2 = this.m.triangleNormals[t2];

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

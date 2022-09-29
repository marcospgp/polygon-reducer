using System;
using System.Collections.Generic;
using MarcosPereira.UnityUtilities;
using UnityEngine;

namespace MarcosPereira.PolygonReducer {
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

        [SerializeField]
        private Cost cost;

        public Collapser(ExtendedMesh m, Cost cost) {
            this.m = m;
            this.cost = cost;

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

            int currentVertexCount = vertexCount - this.m.deletedVertices.Count;
            int targetVertexCount = this.GetTargetVertexCount(reductionFactor);

            if (currentVertexCount > targetVertexCount) {
                // Create new vertex collapse steps, or reapply previously
                // calculated ones if available, until target vertex count is
                // reached.
                while (
                    vertexCount - this.m.deletedVertices.Count >
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
                    vertexCount - this.m.deletedVertices.Count <
                    targetVertexCount
                ) {
                    this.UndoCollapseStep();
                }
            }
        }

        private int GetTargetVertexCount(float reductionFactor) {
            int vertexCount = this.m.vertices.Length;
            int untouchableCount = this.m.seams.Count;
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
            if (uTriangles.Count == 0) {
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

                    float cost = this.cost.Get(u, v, this.m);

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
    }
}

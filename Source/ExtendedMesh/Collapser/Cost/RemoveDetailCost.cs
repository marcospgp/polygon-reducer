using UnityEngine;

namespace MarcosPereira.PolygonReducer.CostFunctions {
    [System.Serializable]
    public class RemoveDetailCost : ICost {
        // This cost function was designed to remove sharp bumps in terrain
        // meshes, resulting from smoothing an initial voxel based mesh (which
        // only has right angles).
        // It is basically the reverse of the default cost function - it
        // prioritizes removing detail.
        public float Get(int from, int to, ExtendedMesh m) {
            Vector3 fromPos = m.vertices[from];
            Vector3 toPos = m.vertices[to];

            float edgeLength = (fromPos - toPos).magnitude;

            // We want to assign a lower cost to edges that are shorter and
            // sharper.
            return (1f / edgeLength) * (1f - Utility.Curvature(from, to, m));
        }
    }
}

using UnityEngine;

namespace MarcosPereira.PolygonReducer.CostFunctions {
    [System.Serializable]
    public class PreserveDetailCost : ICost {
        // This is the default vertex collapse cost function, based on the
        // magazine article that Polygon Reducer is based on.
        // It tries to preserve detail, prioritizing the removal of vertices
        // whose triangles are more coplanar.
        public float Get(int from, int to, ExtendedMesh m) {
            Vector3 fromPos = m.vertices[from];
            Vector3 toPos = m.vertices[to];

            float edgeLength = (fromPos - toPos).magnitude;

            return edgeLength * Utility.Curvature(from, to, m);
        }
    }
}

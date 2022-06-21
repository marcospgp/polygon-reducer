using UnityEngine;

namespace MarcosPereira {
    // This class is used to display ExtendedMesh information in the inspector.
    // It is easier than serializing the ExtendedMesh class itself.
    [System.Serializable]
    public class ExtendedMeshInfo {
        [SerializeField]
        private string originalMeshName;
        [SerializeField]
        private int originalVertexCount;
        [SerializeField]
        private int reducedVertexCount;
        [SerializeField]
        private int originalTriangleCount;
        [SerializeField]
        private int reducedTriangleCount;
        [SerializeField]
        private int seamVertexCount;

        public ExtendedMeshInfo(ExtendedMesh extendedMesh) {
            this.originalMeshName = extendedMesh.originalMesh.name;

            int vertexCount = extendedMesh.originalMesh.vertices.Length;
            int triangleCount = extendedMesh.originalMesh.triangles.Length;

            this.originalVertexCount = vertexCount;
            this.reducedVertexCount =
                vertexCount - extendedMesh.deletedVertices.count;

            this.originalTriangleCount = triangleCount;
            this.reducedTriangleCount =
                triangleCount - extendedMesh.deletedTriangles.count;

            this.seamVertexCount = extendedMesh.seams.count;
        }
    }
}

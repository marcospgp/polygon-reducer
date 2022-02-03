using UnityEngine;

namespace MarcosPereira.MeshManipulation {
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
                vertexCount - extendedMesh.deletedVertices.Count;

            this.originalTriangleCount = triangleCount;
            this.reducedTriangleCount =
                triangleCount - extendedMesh.deletedTriangles.Count;

            this.seamVertexCount = extendedMesh.seams.Count;
        }
    }
}

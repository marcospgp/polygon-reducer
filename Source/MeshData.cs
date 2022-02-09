using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    [System.Serializable]
    public class MeshData {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public MeshFilter meshFilter;
        public Mesh reducedMesh;
        public ExtendedMesh extendedMesh;
        public ExtendedMeshInfo extendedMeshInfo;
    }
}

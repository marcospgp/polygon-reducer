using UnityEngine;

namespace MarcosPereira {
    [System.Serializable]
    public class MeshData {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public MeshFilter meshFilter;
        public Mesh reducedMesh;
        public ExtendedMesh extendedMesh;
        public ExtendedMeshInfo extendedMeshInfo;
    }
}

using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    public class MeshData : ScriptableObject {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public MeshFilter meshFilter;
        public Mesh reducedMesh;
        public ExtendedMesh extendedMesh;
        public ExtendedMeshInfo extendedMeshInfo;
    }
}

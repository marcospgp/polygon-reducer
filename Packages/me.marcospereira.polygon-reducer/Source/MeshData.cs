using System;
using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    [Serializable]
    public class MeshData {
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public MeshFilter meshFilter;
        public Mesh originalMesh;
        public Mesh reducedMesh;
        public ExtendedMesh extendedMesh;
        public ExtendedMeshInfo extendedMeshInfo;
    }
}

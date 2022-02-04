using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    [System.Serializable]
    public struct Vector3Pair {
        public readonly Vector3 a;
        public readonly Vector3 b;

        public Vector3Pair(Vector3 a, Vector3 b) {
            this.a = a;
            this.b = b;
        }
    }
}

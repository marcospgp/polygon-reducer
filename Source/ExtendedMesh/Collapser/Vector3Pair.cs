using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    // Would prefer this struct to be readonly, but then it wouldn't be
    // serializable.
    [System.Serializable]
    public struct Vector3Pair {
        public Vector3 a;
        public Vector3 b;

        public Vector3Pair(Vector3 a, Vector3 b) {
            this.a = a;
            this.b = b;
        }
    }
}

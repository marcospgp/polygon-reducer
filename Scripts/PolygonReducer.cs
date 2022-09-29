using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.PolygonReducer {
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PolygonReducer : MonoBehaviour {
        [Range(0f, 100f)]
        [Tooltip(
            "Set the percentage of vertices to be collapsed. Seam vertices " +
            "are ignored, as removing them would create holes in the mesh."
        )]
        public float reductionPercent = 20f;

        // Keep a static dictionary with original Mesh references, so that only
        // a single ExtendedMesh is created for each Mesh even if this script
        // is present in multiple gameobjects with the same mesh.
        // TODO: handle serialization of this thing
        private static readonly Dictionary<Mesh, ExtendedMesh> extendedMeshCache =
            new Dictionary<Mesh, ExtendedMesh>();

        [SerializeField, HideInInspector]
        private List<MeshData> meshData;

        // Work in progress, was not actually implemented
        // [SerializeField]
        // private ReductionMethod method;

        [SerializeField]
        private List<ExtendedMeshInfo> details;

        private Coroutine inspectorCoroutine;

        // Work in progress, was not actually implemented
        // [System.Serializable]
        // public enum ReductionMethod : byte {
        //     Default,
        //     RemoveDetail
        // }

        // public void OnDrawGizmos() {
        //     // Highlight seams (vertices that cannot be collapsed due to
        //     // being part of a mesh seam)
        //     foreach (MeshData m in this.meshData) {
        //         ExtendedMesh extendedMesh = m.extendedMesh;
        //         Gizmos.color = Color.red;
        //
        //         foreach (int i in extendedMesh.seams) {
        //             Gizmos.DrawSphere(
        //                 this.transform.TransformPoint(
        //                     extendedMesh.vertices[i]
        //                 ),
        //                 0.003f
        //             );
        //         }
        //     }
        // }

        public void Awake() {
            if (this.DestroyIfDuplicate()) {
                return;
            }

            if (this.meshData == null) {
                this.meshData = this.GetMeshData();
            }

            if (this.details == null) {
                this.details = new List<ExtendedMeshInfo>();

                foreach (MeshData meshData in this.meshData) {
                    this.details.Add(meshData.extendedMeshInfo);
                }
            }
        }

        public void OnEnable() {
            // Use a coroutine instead of Update() for efficiency - once we want
            // to stop updating the mesh we can simply stop the coroutine, while
            // removing the overhead of Update() completely seems impossible.
            // TODO: try to optimize this.
            if (this.inspectorCoroutine != null) {
                this.StopCoroutine(this.inspectorCoroutine);
            }

            this.inspectorCoroutine = this.StartCoroutine(this.Monitor());
        }

        public void OnDisable() {
            // Stop coroutine
            if (this.inspectorCoroutine != null) {
                this.StopCoroutine(this.inspectorCoroutine);
                this.inspectorCoroutine = null;
            }
        }

        public IEnumerator Monitor() {
            // Set to -1f so mesh is updated right away
            float lastReductionPercent = -1f;

            while (true) {
                if (this.reductionPercent == lastReductionPercent) {
                    // Cannot wait longer than one frame here due to how editor mode
                    // only processes frames as needed. Waiting for 100ms caused an
                    // issue where changing reduction percent in inspector too fast
                    // did not update the mesh according to the latest value.
                    yield return null;
                    continue;
                }

                this.details.Clear();

                foreach (MeshData meshData in this.meshData) {
                    // TODO: This generates a lot of memory garbage :/
                    meshData.reducedMesh =
                        meshData.extendedMesh.GetMesh(this.reductionPercent);

                    if (meshData.meshFilter != null) {
                        meshData.meshFilter.sharedMesh = meshData.reducedMesh;
                    } else if (meshData.skinnedMeshRenderer != null) {
                        meshData.skinnedMeshRenderer.sharedMesh =
                            meshData.reducedMesh;
                    }

                    // TODO: remove
                    meshData.extendedMeshInfo =
                        new ExtendedMeshInfo(meshData.extendedMesh);
                    this.details.Add(meshData.extendedMeshInfo);
                }

                lastReductionPercent = this.reductionPercent;
            }
        }

        private static ExtendedMesh GetExtendedMesh(Mesh mesh) {
            if (
                !PolygonReducer.extendedMeshCache
                    .TryGetValue(mesh, out ExtendedMesh extendedMesh)
            ) {
                extendedMesh = ExtendedMesh.Create(mesh);
                PolygonReducer.extendedMeshCache.Add(mesh, extendedMesh);
            }

            return extendedMesh;
        }

        private bool DestroyIfDuplicate() {
            var existing = new List<PolygonReducer>();

            existing.AddRange(
                this.gameObject.GetComponentsInChildren<PolygonReducer>()
            );
            existing.AddRange(
                this.gameObject.GetComponentsInParent<PolygonReducer>()
            );

            GameObject containing = null;

            foreach (PolygonReducer x in existing) {
                if (x != this && x.enabled) {
                    containing = x.gameObject;
                    break;
                }
            }

            if (containing == null) {
                return false;
            }

            Debug.LogError(
                "Only one PolygonReducer per object hierarchy is supported. " +
                "Please remove the PolygonReducer component from " +
                $"\"{containing.name}\" before adding it to " +
                $"\"{this.gameObject.name}\"."
            );

            if (Application.isPlaying) {
                GameObject.Destroy(this);
            } else {
                Object.DestroyImmediate(this, allowDestroyingAssets: false);
            }

            return true;
        }

        private List<MeshData> GetMeshData() {
            var meshData = new List<MeshData>();

            static void LogReadWriteError(string meshName) =>
                Debug.LogError(
                    $"Polygon Reducer cannot read mesh \"{meshName}\". " +
                    "Please enable the \"Read/Write\" checkbox in the mesh's " +
                    "import settings."
                );

            static MeshData F(Mesh mesh) {
                if (!mesh.isReadable) {
                    LogReadWriteError(mesh.name);
                    return null;
                }

                ExtendedMesh extendedMesh = PolygonReducer.GetExtendedMesh(mesh);

                return new MeshData() {
                    extendedMesh = extendedMesh,
                    extendedMeshInfo = new ExtendedMeshInfo(extendedMesh)
                };
            }

            MeshFilter[] meshFilters = this.gameObject
                .GetComponentsInChildren<MeshFilter>(includeInactive: true);

            foreach (MeshFilter meshFilter in meshFilters) {
                MeshData item = F(meshFilter.sharedMesh);

                if (item == null) {
                    continue;
                }

                item.meshFilter = meshFilter;
                meshData.Add(item);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = this.gameObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(
                    includeInactive: true
                );

            foreach (SkinnedMeshRenderer skinnedMeshRenderer in skinnedMeshRenderers) {
                MeshData item = F(skinnedMeshRenderer.sharedMesh);

                if (item == null) {
                    continue;
                }

                item.skinnedMeshRenderer = skinnedMeshRenderer;
                meshData.Add(item);
            }

            return meshData;
        }
    }
}

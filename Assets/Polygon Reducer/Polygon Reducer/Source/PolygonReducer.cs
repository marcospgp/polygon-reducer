using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MarcosPereira.MeshManipulation {
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PolygonReducer : MonoBehaviour {
        // Keep a static dictionary with original Mesh references, so that only
        // a single ExtendedMesh is created for each Mesh even if this script
        // is present in multiple gameobjects with the same mesh.
        private static readonly Dictionary<Mesh, ExtendedMesh> extendedMeshCache =
            new Dictionary<Mesh, ExtendedMesh>();

        [Range(0f, 100f)]
        [Tooltip(
            "Set the percentage of vertices to be collapsed. Seam vertices " +
            "are ignored, as removing them would create holes in the mesh."
        )]
        public float reductionPercent = 20f;

        [SerializeField, HideInInspector]
        private List<MeshData> meshData;

        [SerializeField]
        private List<ExtendedMeshInfo> details;

        private Coroutine inspectorCoroutine;

        // Debugging fields
        [SerializeField]
        [Tooltip(
            "Highlight vertices that cannot be collapsed due to being part " +
            "of a mesh seam."
        )]
        private bool highlightSeams = false;

        public void OnDrawGizmos() {
            if (this.highlightSeams) {
                foreach (MeshData m in this.meshData) {
                    ExtendedMesh extendedMesh = m.extendedMesh;
                    Gizmos.color = Color.red;

                    foreach (int i in extendedMesh.seams) {
                        Gizmos.DrawSphere(
                            this.transform.TransformPoint(
                                extendedMesh.vertices[i]
                            ),
                            0.003f
                        );
                    }
                }
            }
        }

        // We used to reduce meshes in OnEnable and restore them in OnDisable, but there were
        // multiple issues with this. When entering play mode, OnEnable sees an already enabled
        // state after deserialization. Additionally, when polygon reducer is enabled on a prefab,
        // and the folder that prefab is in is renamed, OnEnable sees the already reduced mesh in
        // the mesh filter or skinned mesh renderer's sharedMesh - even if OnDisable restored it
        // to its original.
        public void Awake() {
            if (this.DestroyIfDuplicate()) {
                return;
            }

            this.meshData = this.GetMeshData();

            this.details = new List<ExtendedMeshInfo>();

            foreach (MeshData meshData in this.meshData) {
                this.details.Add(meshData.extendedMeshInfo);
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

                    meshData.extendedMeshInfo =
                        new ExtendedMeshInfo(meshData.extendedMesh);

                    this.details.Add(meshData.extendedMeshInfo);
                }

                lastReductionPercent = this.reductionPercent;
            }
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

            GameObject.Destroy(this);

            return true;
        }

        private List<MeshData> GetMeshData() {
            var meshData = new List<MeshData>();

            static void logReadWriteError(string meshName) =>
                Debug.LogError(
                    $"Polygon Reducer cannot modify mesh \"{meshName}\" - " +
                    "please enable the \"Read/Write Enabled\" " +
                    "checkbox in the mesh's import settings."
                );

            static MeshData F(Mesh mesh) {
                if (!mesh.isReadable) {
                    logReadWriteError(mesh.name);
                    return null;
                }

                if (mesh.name.Contains("(reduced)")) {
                    Debug.LogWarning(
                        "Polygon Reducer: Skipping mesh with \"(reduced)\" in name. " +
                        "Reducing an already reduced mesh makes it impossible to undo the " +
                        "reduction to less than the current level. If this is happening in a " +
                        "prefab, try resetting it."
                    );

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

        private static ExtendedMesh GetExtendedMesh(Mesh mesh) {
            if (
                !PolygonReducer.extendedMeshCache
                    .TryGetValue(mesh, out ExtendedMesh extendedMesh)
            ) {
                extendedMesh = new ExtendedMesh(mesh);
                PolygonReducer.extendedMeshCache.Add(mesh, extendedMesh);
            }

            return extendedMesh;
        }
    }
}

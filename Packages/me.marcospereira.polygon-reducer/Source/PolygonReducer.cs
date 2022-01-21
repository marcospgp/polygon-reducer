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
        private static readonly Dictionary<
            Mesh, ExtendedMesh
        > extendedMeshCache = new Dictionary<Mesh, ExtendedMesh>();

        [Range(0f, 100f)]
        [Tooltip(
            "Set the percentage of vertices to be collapsed. Seam vertices " +
            "are ignored, as removing them would create holes in the mesh."
        )]
        public float reductionPercent = 20f;

        [SerializeField, HideInInspector]
        private List<MeshData> meshData;

        private Coroutine inspectorCoroutine;

        // Debugging fields
        [SerializeField]
        [Tooltip(
            "Highlight vertices that cannot be collapsed due to being part " +
            "of a mesh seam."
        )]
        private bool highlightSeams = false;

        // When entering play mode, editor scripts see serialized values of an
        // enabled state in OnEnable(). To avoid running OnEnable() logic on
        // values corresponding to an already enabled state, we keep track of
        // enabled state here. Serializing this field is critical.
        // OnEnable() will see this value as true right before the
        // EnteredEditMode event is triggered.
        // Forum thread here:
        // https://forum.unity.com/threads/onenable-sees-serialized-values-of-enabled-state-on-monobehaviour-with-executealways.1198969/
        // More info at:
        // https://docs.unity3d.com/2021.2/Documentation/Manual/ConfigurableEnterPlayModeDetails.html
        // https://docs.unity3d.com/2021.2/Documentation/Manual/ConfigurableEnterPlayMode.html
        [SerializeField, HideInInspector]
        private bool isInitialized = false;

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

        public void OnEnable() {
            if (this.DisableIfDuplicate()) {
                return;
            }

            // When entering play mode, we see serialized values of an enabled
            // state. We do not fetch and reduce meshes then, or we will be
            // reducing already reduced meshes.
            if (!this.isInitialized) {
                this.isInitialized = true;

                // Populate original meshes
                this.meshData = this.LoadMeshes();
            }

            // TODO: Get extended meshes in Populate() instead once they are
            // serialized.

            foreach (MeshData meshData in this.meshData) {
                meshData.extendedMesh =
                    PolygonReducer.GetExtendedMesh(meshData.originalMesh);
                meshData.extendedMeshInfo =
                    new ExtendedMeshInfo(meshData.extendedMesh);
            }

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
            if (!this.isInitialized) {
                // Happens when this component is disabled in OnEnable() due to
                // being a duplicate.
                return;
            }

            this.isInitialized = false;

            // Restore original meshes, unless the reduced mesh has been
            // replaced in the meantime.
            foreach (MeshData meshData in this.meshData) {
                this.RestoreMesh(meshData);
            }

            // Stop coroutine
            if (this.inspectorCoroutine != null) {
                this.StopCoroutine(this.inspectorCoroutine);
            }

            // Clear mesh data
            this.meshData.Clear();
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
                }

                lastReductionPercent = this.reductionPercent;
            }
        }

        private bool DisableIfDuplicate() {
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
                "Please disable the PolygonReducer component on " +
                $"\"{containing.name}\" before enabling it on " +
                $"\"{this.gameObject.name}\"."
            );

            this.enabled = false;

            return true;
        }

        private List<MeshData> LoadMeshes() {
            var meshData = new List<MeshData>();

            static void logReadWriteError(string meshName) =>
                Debug.LogError(
                    $"Polygon Reducer cannot modify mesh \"{meshName}\" - " +
                    "please enable the \"Read/Write Enabled\" " +
                    "checkbox in the mesh's import settings."
                );

            MeshFilter[] meshFilters = this.gameObject
                .GetComponentsInChildren<MeshFilter>(includeInactive: true);

            foreach (MeshFilter meshFilter in meshFilters) {
                Mesh mesh = meshFilter.sharedMesh;

                if (!mesh.isReadable) {
                    logReadWriteError(mesh.name);
                    continue;
                }

                meshData.Add(new MeshData() {
                    originalMesh = mesh,
                    meshFilter = meshFilter
                });
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = this.gameObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(
                    includeInactive: true
                );

            foreach (SkinnedMeshRenderer x in skinnedMeshRenderers) {
                Mesh mesh = x.sharedMesh;

                if (!mesh.isReadable) {
                    logReadWriteError(mesh.name);
                    continue;
                }

                meshData.Add(new MeshData() {
                    originalMesh = mesh,
                    skinnedMeshRenderer = x
                });
            }

            return meshData;
        }

        private void RestoreMesh(MeshData meshData) {
            string errorMessage =
                "Polygon Reducer: Did not restore mesh " +
                $"\"{meshData.originalMesh.name}\"" +
                " as it has been replaced after having been optimized.";

            if (meshData.meshFilter != null) {
                MeshFilter f = meshData.meshFilter;

                if (
                    f.sharedMesh.GetInstanceID() ==
                        meshData.reducedMesh.GetInstanceID()
                ) {
                    f.sharedMesh = meshData.originalMesh;
                } else {
                    Debug.LogError(errorMessage);
                }
            } else if (meshData.skinnedMeshRenderer != null) {
                SkinnedMeshRenderer r = meshData.skinnedMeshRenderer;

                if (
                    r.sharedMesh.GetInstanceID() ==
                        meshData.reducedMesh.GetInstanceID()
                ) {
                    r.sharedMesh = meshData.originalMesh;
                } else {
                    Debug.LogError(errorMessage);
                }
            } else {
                throw new System.Exception("Unexpected null components.");
            }
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

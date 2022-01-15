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
#pragma warning disable RCS0056 // A line is too long
        private static readonly Dictionary<Mesh, ExtendedMesh> extendedMeshCache =
            new Dictionary<Mesh, ExtendedMesh>();
#pragma warning restore

        [Range(0f, 100f)]
        [Tooltip(
            "Set the percentage of vertices to be collapsed. Seam vertices " +
            "are ignored, as removing them would create holes in the mesh."
        )]
        public float reductionPercent = 20f;

        [SerializeField, HideInInspector]
        private MeshFilter[] meshFilters;

        [SerializeField, HideInInspector]
        private SkinnedMeshRenderer[] skinnedMeshRenderers;

        // Original mesh references.
        // MeshFilter meshes come first, then SkinnedMeshRenderer meshes.
        [SerializeField, HideInInspector]
        private List<Mesh> originalMeshes;

        // Stores a reference to each original mesh's respective
        // polygon-reduced mesh.
        [SerializeField, HideInInspector]
        private Mesh[] reducedMeshes;

        // Used to retrieve new meshes at a desired quality level.
        // TODO: serialize this
        private ExtendedMesh[] extendedMeshes;

        // Extended mesh information. Used for display in custom inspector,
        // while the ExtendedMesh class itself is not serializable (which is a
        // TODO).
        [SerializeField]
        private ExtendedMeshInfo[] details;

        private Coroutine inspectorCoroutine;

        // Debugging fields - uncomment to display in inspector.
        // [Header("Debugging")]
        // [SerializeField]
        private bool highlightSeams = false;
        // [SerializeField]
        private bool verboseLogging = false;

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
                foreach (ExtendedMesh extendedMesh in this.extendedMeshes) {
                    Gizmos.color = Color.red;

                    foreach (int i in extendedMesh.seams) {
                        Gizmos.DrawSphere(
                            this.transform.TransformPoint(
                                extendedMesh.vertices[i]
                            ),
                            0.01f
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
                this.Populate();
            }

            // TODO: Get extended meshes in Populate() instead once they are
            // serialized.
            this.extendedMeshes = new ExtendedMesh[this.originalMeshes.Count];
            this.details =
                new ExtendedMeshInfo[this.originalMeshes.Count];
            for (int i = 0; i < this.extendedMeshes.Length; i++) {
                this.extendedMeshes[i] =
                    PolygonReducer.GetExtendedMesh(this.originalMeshes[i]);

                this.details[i] =
                    new ExtendedMeshInfo(this.extendedMeshes[i]);
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

            // Read more about this.isInitialized above.
            this.isInitialized = false;

            // Restore original meshes, unless the reduced mesh has been
            // replaced in the meantime.
            for (int i = 0; i < this.originalMeshes.Count; i++) {
                if (this.originalMeshes[i] == null) {
                    // An original mesh may be null, such as if it is not
                    // read/write enabled.
                    continue;
                }

                this.SetMesh(
                    i,
                    this.originalMeshes[i],
                    ifEquals: this.reducedMeshes[i]
                );
            }

            // Stop coroutine
            if (this.inspectorCoroutine != null) {
                this.StopCoroutine(this.inspectorCoroutine);
            }

            // Clear lists
            this.meshFilters = null;
            this.skinnedMeshRenderers = null;
            this.originalMeshes = null;
            this.reducedMeshes = null;
            this.extendedMeshes = null;
            this.details = null;
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

                for (int i = 0; i < this.originalMeshes.Count; i++) {
                    // TODO: This generates a lot of memory garbage :/
                    Mesh reducedMesh = this.extendedMeshes[i]
                        .GetMesh(this.reductionPercent);

                    this.SetMesh(i, reducedMesh);

                    // Update extended mesh info for inspector
                    // TODO: maybe it should be a struct to avoid garbage?
                    this.details[i] =
                        new ExtendedMeshInfo(this.extendedMeshes[i]);

                    // Store reference to the new polygon-reduced mesh
                    this.reducedMeshes[i] = reducedMesh;
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

        private void Populate() {
            this.meshFilters = this.gameObject
                .GetComponentsInChildren<MeshFilter>(includeInactive: true);

            this.skinnedMeshRenderers = this.gameObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(
                    includeInactive: true
                );

            this.originalMeshes = this.GetOriginalMeshes();

            this.reducedMeshes = new Mesh[this.originalMeshes.Count];
        }

        private void SetMesh(int i, Mesh mesh, Mesh ifEquals = null) {
            string nullError =
                "Polygon Reducer could not set mesh of " +
                $"gameObject \"{this.gameObject.name}\" or one of its " +
                "children. Missing reference to a ";

            if (i < this.meshFilters.Length) {
                MeshFilter f = this.meshFilters[i];

                if (f == null) {
                    this.LogVerbose($"{nullError} MeshFilter.", isError: true);
                    return;
                }

                if (ifEquals == null || f.sharedMesh == ifEquals) {
                    // Important: use .sharedMesh, not .mesh. The latter is a hacky
                    // cloning placeholder kind of thing.
                    f.sharedMesh = mesh;
                } else {
                    this.LogVerbose(
                        "Polygon Reducer did not set mesh as it seems to " +
                        "have been replaced."
                    );
                }
            } else {
                int j = i - this.meshFilters.Length;
                SkinnedMeshRenderer r = this.skinnedMeshRenderers[j];

                if (r == null) {
                    this.LogVerbose(
                        $"{nullError} SkinnedMeshRenderer.",
                        isError: true
                    );
                    return;
                }

                if (ifEquals == null || r.sharedMesh == ifEquals) {
                    r.sharedMesh = mesh;
                } else {
                    this.LogVerbose(
                        "Polygon Reducer did not set mesh as it seems to " +
                        "have been replaced."
                    );
                }
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

        private List<Mesh> GetOriginalMeshes() {
            var originalMeshes = new List<Mesh>();

            void StoreMesh(Mesh mesh) {
                if (mesh != null) {
                    if (!mesh.isReadable) {
                        this.LogVerbose(
                            "Polygon Reducer cannot modify mesh " +
                            $"\"{mesh.name}\" because it is not readable. " +
                            "Please enable the \"Read/Write Enabled\" " +
                            "checkbox in the mesh's import settings.",
                            isError: true
                        );

                        // Store a null mesh to ensure indices match between
                        // meshes and MeshFilters/SkinnedMeshRenderers
                        originalMeshes.Add(null);
                    }

                    originalMeshes.Add(mesh);
                }
            }

            if (this.meshFilters != null) {
                foreach (MeshFilter meshFilter in this.meshFilters) {
                    // Don't use MeshFilter.mesh here! It will leak meshes if
                    // called in the editor, and is unecessary since we do not
                    // modify the mesh directly.
                    StoreMesh(meshFilter.sharedMesh);
                }
            }

            if (this.skinnedMeshRenderers != null) {
                foreach (SkinnedMeshRenderer r in this.skinnedMeshRenderers) {
                    StoreMesh(r.sharedMesh);
                }
            }

            if (originalMeshes.Count == 0) {
                throw new System.Exception(
                    "Polygon Reducer could not find a mesh on gameobject " +
                    $"\"{this.gameObject.name}\" or on any of its children. " +
                    "Are you missing a MeshFilter or SkinnedMeshRenderer?"
                );
            }

            return originalMeshes;
        }

        private void LogVerbose(string message, bool isError = false) {
            if (this.verboseLogging) {
                if (isError) {
                    Debug.LogError(message);
                } else {
                    Debug.Log(message);
                }
            }
        }
    }
}

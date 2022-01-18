using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MarcosPereira.MeshManipulation {
    [CustomEditor(typeof(PolygonReducer))]
    public class PolygonReducerInspector : Editor {
        public override VisualElement CreateInspectorGUI() {
            var inspector = new VisualElement();

            // Display default inspector - only available starting with version
            // 2021 of Unity.
            // InspectorElement.FillDefaultInspector(
            //     inspector,
            //     this.serializedObject,
            //     this
            // );

            inspector.Add(
                new PropertyField(
                    this.serializedObject.FindProperty("reductionPercent")
                )
            );

            SerializedProperty detailsArray =
                this.serializedObject.FindProperty("details");

            if (detailsArray == null) {
                Debug.LogError(
                    "Polygon Reducer: Missing field for custom inspector."
                );
            }

            var foldout = new Foldout() {
                text = detailsArray.displayName
            };
            inspector.Add(foldout);

            foldout.Add(new Label(
                $"Found {detailsArray.arraySize} meshes in this GameObject " +
                "and its children.\n"
            ));

            for (int i = 0; i < detailsArray.arraySize; i++) {
                foldout.Add(
                    new PropertyField(detailsArray.GetArrayElementAtIndex(i))
                );
            }

            var debugFoldout = new Foldout() {
                text = "Debug"
            };
            inspector.Add(debugFoldout);

            SerializedProperty highlightSeams =
                this.serializedObject.FindProperty("highlightSeams");

            debugFoldout.Add(
                new PropertyField(highlightSeams) {
                    tooltip = highlightSeams.tooltip
                }
            );

            return inspector;
        }
    }
}

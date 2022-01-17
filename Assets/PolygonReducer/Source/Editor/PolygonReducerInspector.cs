using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MarcosPereira.MeshManipulation {
    [CustomEditor(typeof(PolygonReducer))]
    public class PolygonReducerInspector : Editor {
        public override VisualElement CreateInspectorGUI() {
            var inspector = new VisualElement();

            // Display default inspector
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

            SerializedProperty array =
                this.serializedObject.FindProperty("details");

            if (array == null) {
                Debug.LogError(
                    "Polygon Reducer: Missing field for custom inspector."
                );
            }

            var foldout = new Foldout() {
                text = array.displayName
            };
            inspector.Add(foldout);

            foldout.Add(new Label(
                $"Found {array.arraySize} meshes in this GameObject and its " +
                "children."
            ));

            for (int i = 0; i < array.arraySize; i++) {
                foldout.Add(
                    new PropertyField(array.GetArrayElementAtIndex(i))
                );
            }

            return inspector;
        }
    }
}

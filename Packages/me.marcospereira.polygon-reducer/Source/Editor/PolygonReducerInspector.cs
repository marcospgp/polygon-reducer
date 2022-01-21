using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace MarcosPereira.MeshManipulation {
    [CustomEditor(typeof(PolygonReducer))]
    public class PolygonReducerInspector : Editor {
        public override VisualElement CreateInspectorGUI() {
            var inspector = new VisualElement();

            // Reduction percent setting
            inspector.Add(
                new PropertyField(
                    this.serializedObject.FindProperty("reductionPercent")
                )
            );

            // Details foldout

            SerializedProperty meshData =
                this.serializedObject.FindProperty("meshData");

            var detailsList = new List<SerializedProperty>();

            System.Collections.IEnumerator enumerator =
                meshData.GetEnumerator();

            while (enumerator.MoveNext()) {
                meshData = enumerator.Current as SerializedProperty;
                detailsList.Add(
                    meshData.FindPropertyRelative("extendedMeshInfo")
                );
            }

            PropertyField details = new PropertyField(
                SerializedProperty.
            );

            inspector.Add(details);

            // Wait for property field to be populated before modifying it.
            // Source: https://forum.unity.com/threads/solved-how-to-force-update-visual-element-on-the-current-frame.727040/#post-4984787
            _ = details.schedule.Execute(() => {
                // Get size field of details array
                IntegerField sizeField = details.Q<IntegerField>();

                // Disallow changing array size in inspector
                sizeField.SetEnabled(false);

                sizeField.label = "Meshes found";

                // The tooltip doesn't work when the IntegerField has been
                // disabled, sadly. I tried disabling only the inner input
                // instead, which made the tooltip work, but then the size value
                // could still be changed by clicking and dragging the mouse.
                sizeField.tooltip =
                    "The number of meshes found in this GameObject and its " +
                    "children.";
            });

            // Debug foldout
            // Work in progress - uncomment to display

            // var debugFoldout = new Foldout() {
            //     text = "Debug",
            //     value = false // Collapsed by default
            // };
            // inspector.Add(debugFoldout);

            // SerializedProperty highlightSeams =
            //     this.serializedObject.FindProperty("highlightSeams");

            // debugFoldout.Add(
            //     new PropertyField(highlightSeams) {
            //         tooltip = highlightSeams.tooltip
            //     }
            // );

            return inspector;
        }
    }
}

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MarcosPereira.PolygonReducer {
    [CustomEditor(typeof(PolygonReducer))]
    public class PolygonReducerInspector : Editor {
        [SerializeField]
        private VisualTreeAsset inspectorXML;

        public override VisualElement CreateInspectorGUI() {
            if (this.inspectorXML == null) {
                throw new System.Exception("Polygon Reducer: Missing inspector XML.");
            }

            var inspector = new VisualElement();

            this.inspectorXML.CloneTree(inspector);

            // InspectorElement.FillDefaultInspector(inspector, this.serializedObject, this);

            // // Reduction percent setting
            // inspector.Add(
            //     new PropertyField(
            //         this.serializedObject.FindProperty("reductionPercent")
            //     )
            // );

            // // Details foldout

            // var details = new PropertyField(
            //     this.serializedObject.FindProperty("details")
            // );

            // inspector.Add(details);

            PropertyField details = inspector.Query<PropertyField>("Details");

            // Wait for property field to be populated before modifying it.
            // Source: https://forum.unity.com/threads/solved-how-to-force-update-visual-element-on-the-current-frame.727040/#post-4984787
            _ = details.schedule.Execute(() => {
                // Disallow changing array size in inspector

                IntegerField sizeField = details.Query<IntegerField>();

                if (sizeField != null) {
                    sizeField.SetEnabled(false);
                }

                TextField textField = details.Query<TextField>();

                if (textField != null) {
                    textField.SetEnabled(false);
                }
            });

            // Debug foldout

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

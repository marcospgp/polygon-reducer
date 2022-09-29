using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MarcosPereira.PolygonReducer {
    [CustomEditor(typeof(PolygonReducer))]
    public class PolygonReducerInspector : Editor {
        public override VisualElement CreateInspectorGUI() {
            var inspector = new VisualElement();

            InspectorElement.FillDefaultInspector(inspector, this.serializedObject, this);

            PropertyField scriptField = inspector.Q<PropertyField>("PropertyField:m_Script");

            if (scriptField != null) {
                scriptField.style.display = DisplayStyle.None;
            }

            PropertyField details = inspector.Q<PropertyField>("PropertyField:details");

            if (details != null) {
                details.SetEnabled(false);
            }

            return inspector;
        }
    }
}

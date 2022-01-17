using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace MarcosPereira.MeshManipulation {
    [CustomPropertyDrawer(typeof(ExtendedMeshInfo))]
    public class ExtendedMeshInfoPropertyDrawer : PropertyDrawer {
        public override VisualElement CreatePropertyGUI(
            SerializedProperty property
        ) {
            var foldout = new Foldout();

            // Align with default inspector fields
            foldout.style.marginLeft =
                new StyleLength(new Length(3, LengthUnit.Pixel));

            SerializedProperty originalMeshName = property
                .FindPropertyRelative("originalMeshName");
            SerializedProperty originalVertexCount = property
                .FindPropertyRelative("originalVertexCount");
            SerializedProperty reducedVertexCount = property
                .FindPropertyRelative("reducedVertexCount");
            SerializedProperty originalTriangleCount = property
                .FindPropertyRelative("originalTriangleCount");
            SerializedProperty reducedTriangleCount = property
                .FindPropertyRelative("reducedTriangleCount");
            SerializedProperty seamVertexCount = property
                .FindPropertyRelative("seamVertexCount");

            if (
                originalMeshName == null ||
                originalVertexCount == null ||
                reducedVertexCount == null ||
                originalTriangleCount == null ||
                reducedTriangleCount == null ||
                seamVertexCount == null
            ) {
                Debug.LogError(
                    "Polygon Reducer: Missing extended mesh information " +
                    "field."
                );

                return foldout;
            }

            foldout.text = originalMeshName.stringValue;

            var a = new Label("Vertex Count");
            a.SetEnabled(false); // Disable to match style of other fields
            a.style.unityFontStyleAndWeight = FontStyle.Bold;
            a.style.marginTop = 4;
            foldout.Add(a);

            var e = new PropertyField(originalVertexCount, "Original");
            var f = new PropertyField(reducedVertexCount, "Reduced");
            var g = new PropertyField(seamVertexCount, "Seams (nonremovable)");
            e.SetEnabled(false);
            f.SetEnabled(false);
            g.SetEnabled(false);
            foldout.Add(e);
            foldout.Add(f);
            foldout.Add(g);

            var b = new Label("Triangle Count");
            b.SetEnabled(false); // Disable to match style of other fields
            b.style.unityFontStyleAndWeight = FontStyle.Bold;
            b.style.marginTop = 4;
            foldout.Add(b);

            var c = new PropertyField(originalTriangleCount, "Original");
            var d = new PropertyField(reducedTriangleCount, "Reduced");
            c.SetEnabled(false);
            d.SetEnabled(false);
            foldout.Add(c);
            foldout.Add(d);

            return foldout;
        }
    }
}
# Editor Folder

Custom inspector files must be inside the Editor folder, or inside an
Editor-only assembly definition. Otherwise, attempting to create standalone
builds will fail, as the UnityEditor namespace isn’t available.

Source: <https://docs.unity3d.com/2021.2/Documentation/Manual/UIE-HowTo-CreateCustomInspector.html>

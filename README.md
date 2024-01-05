# Polygon Reducer

Unity package for reducing mesh vertex counts in real time by dragging a slider.

Vertex collapse algorithm based on [this gdmag article](https://drive.google.com/file/d/18dCjkbG8Yo9b5ypyQliyL7FN1KcEqLC4/view?usp=sharing).

## Dependencies

The following repositories must be found in the Assets folder of the Unity project you want to use Polygon Reducer in:

* <https://github.com/marcospgp/unity-utilities>

## How to use

1. Place this repository in the `Assets` folder of your Unity project.
2. Add the `Polygon Reducer` component to the mesh filter/skinned mesh renderer containing gameobjects you want to optimize.

The component will also detect meshes in child gameobjects.

### Not a local package

This package cannot be installed with the "add package from local folder" option of the Unity package manager. This is to avoid managing an unnecessary `manifest.json` file when we can simply place this package in the `Assets` folder for development.

## How it works

### Algorithm

Polygons are reduced by collapsing vertices one by one into one of their
neighboring vertices. A target vertex is chosen for each vertex based on a
cost formula. We thus have for each vertex a collapse cost and a target vertex.
Vertices are collapsed from the smallest collapse cost to the largest, until a
polygon reduction threshold is reached.
When a vertex is collapsed into another, triangles shared
by the two are removed. Triangles connected to the collapsed vertex have those
connections point to the target vertex instead.

### In Practice

In OnEnable() we replace meshes of the gameObject and its children with reduced
versions. The reduction level is updated in accordance to the slider in the
inspector.
In OnDisable() we restore meshes to their original version unless they have been
replaced in the meantime.

Due to how Unity handles serialization, we have to call OnDisable() before
entering play mode to avoid OnEnable() seeing its own mesh and re-reducing it.

## Running in edit mode and serialization

Unity serializes MonoBehaviour objects as part of its domain reload
([more info here](https://docs.unity3d.com/2021.2/Documentation/Manual/ConfigurableEnterPlayModeDetails.html)).
A deserialization (restoring values from a previous serialization) happens right
before `OnEnable()` when entering play mode.
This means Polygon Reducer may find its own mesh when looking for a mesh to
reduce in `OnEnable()`. We thus have to store a reference to the reduced mesh so
we can check against it and avoid reducing it again.
We also take care to store a reference to the original mesh, so we can restore
it when Polygon Reducer is disabled - unless it has been manually replaced in
the meantime.

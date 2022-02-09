# Polygon Reducer

Unity package for reducing mesh vertex counts in real time by dragging a slider.

Vertex collapse algorithm based on <https://drive.google.com/file/d/18dCjkbG8Yo9b5ypyQliyL7FN1KcEqLC4/view?usp=sharing>

## How to use

Either install this package from the Unity Asset Store or place the `Polygon Reducer` folder of this repository in the `Assets` folder of your Unity project. Git submodules can be used to keep the folder in sync with this repository.
Then, simply place the `Polygon Reducer` component in a gameobject containing a skinned mesh renderer or mesh filter, either on itself or on any of its children.

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

## Possible improvements

### Moving target vertex to center of collapsed edge

One possibility that is not mentioned on the original article describing this
algorithm is moving the target vertex to the middle of the edge that was
collapsed, instead of keeping it in the same place.

The target vertex has to be moved at the beginning of the collapse operation,
before any triangle normals are recalculated.

This would require also recalculating the collapse costs for vertices
connected to the target vertex, after the collapse is processed.

I ended up removing this functionality since it may not be a good idea.
It implies that the cost of collapsing u to v is the same as that of collapsing
v to u, which is not the case. In practice, it seems to result in worse visuals.

Update: one possibility would be to calculate the cost per edge by averaging the
cost of collapsing each vertex into each other.

Update 2: not sure how this would affect UVs.

### Merging bone weights on collapse

Currently, bone weights are simply copied to the new mesh after polygon
reduction, with each undeleted vertex keeping its original bone weights.

It would be possible to keep track of bone weights while collapsing vertices.
When a vertex is collapsed, the target vertex would inherit its bone weights.
This didn't seem necessary, however, as the current implementation works well.

## Notes

### No longer relying on OnEnable/OnDisable

We used to reduce meshes in OnEnable and restore them in OnDisable, but there were
multiple issues with this. When entering play mode, OnEnable sees an already enabled
state after deserialization. Additionally, when polygon reducer is enabled on a prefab,
and the folder that prefab is in is renamed, OnEnable sees the already reduced mesh in
the mesh filter or skinned mesh renderer's sharedMesh - even if OnDisable restored it
to its original.

## TODO

* Fix occasional reduction of already reduced mesh (mesh shows up as `<mesh name> (reduced)` in component's details dropdown, or even `<mesh name> (reduced) (reduced)` and so on.)
* Sphere should have no seams, but has?

maybe in onenable check children gameobjects and reduce any possible new additions?

current issue: entering play mode doesn't work. need to serialize extended meshes or something.

remove extendedmeshinfo now that everything is goddamn serializable

fix extendedmeshcache (populate it in on enable?)

fix serialization of nested (2D) structures. flatten and store flat list + list of sizes of sublists

maybe create a custom class for serializable hashsets and such that handles the serialization/deserialization logic itself?

review polygon reducer logic to see if extended meshes are only generated once

since serializable classes are no longer scriptable objects, make them inherit the respective collection class so that intellisense is good and nice.

In `this.costs = new SerializableSortedSet<Edge>(Costs.CostComparer());`, check if the comparison can be set at the Edge level instead of at the dictionary level.

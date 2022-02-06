# Polygon Reducer

Unity package for reducing mesh vertex counts in real time by dragging a slider.

The package can be found in the `Assets` folder.

## Notes

### No longer relying on OnEnable/OnDisable

We used to reduce meshes in OnEnable and restore them in OnDisable, but there were
multiple issues with this. When entering play mode, OnEnable sees an already enabled
state after deserialization. Additionally, when polygon reducer is enabled on a prefab,
and the folder that prefab is in is renamed, OnEnable sees the already reduced mesh in
the mesh filter or skinned mesh renderer's sharedMesh - even if OnDisable restored it
to its original.

## Unity FAQ

> I changed an assembly definition file and now VSCode won't recognize an import, even though there's no corresponding error in Unity's console.

Solution: Regenerate `.csproj` files (<https://forum.unity.com/threads/intellisense-not-working-for-visual-studio-code.812040/#post-5858986>).

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

can't instantiate generic scriptableobjects. how about using custom class serialization instead, which is supported as of recently, and perhaps use structs instead of classes to make it super clear how unity serialization is storing those values inline.

latest bug: after entering play mode, throws error whenever reduction level is set higher than the highest it was set to while in editor mode.

since serializable classes are no longer scriptable objects, make them inherit the respective collection class so that intellisense is good and nice.

In `this.costs = new SerializableSortedSet<Edge>(Costs.CostComparer());`, check if the comparison can be set at the Edge level instead of at the dictionary level.

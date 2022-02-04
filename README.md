# Polygon Reducer

Unity package for reducing mesh vertex counts in real time by dragging a slider.

The package can be found in the `Assets` folder.

## FAQ

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

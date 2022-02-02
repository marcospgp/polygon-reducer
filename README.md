# Polygon Reducer

Unity package for reducing mesh vertex counts in real time by dragging a slider.

The package can be found in the `Assets` folder.

## TODO

* Fix occasional reduction of already reduced mesh (mesh shows up as `<mesh name> (reduced)` in component's details dropdown, or even `<mesh name> (reduced) (reduced)` and so on.)
* Sphere should have no seams, but has?

maybe in onenable check children gameobjects and reduce any possible new additions?

current issue: entering play mode doesn't work. need to serialize extended meshes or something.

remove extendedmeshinfo now that everything is goddamn serializable

fix extendedmeshcache (populate it in on enable?)

fix serialization of nested (2D) structures. flatten and store flat list + list of sizes of sublists

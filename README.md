# marching-cubes-unity
An implementation of the Marching Cubes algorithm in Unity 3D.
First we define a 3d Grid, splitting it into smaller gridcells.
Using a 3D perlin noise function we assign a noise level each vertex of each gridcell.
Using a "isolevel" as a threadhold value, we compare it to the noise value for each vertex, determining if that point in space is considered terrain or air.
The algorithm "marches" through each cube (gridcell) calculating the triangles to build a mesh.

https://en.wikipedia.org/wiki/Marching_cubes

https://paulbourke.net/geometry/polygonise/
![marchingcubes](https://github.com/ACassiusD/marching-cubes-unity/assets/18119577/6d0725e5-5035-4815-b335-956d196c8343)

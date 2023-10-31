# marching-cubes-unity
An implementation of the Marching Cubes algorithm in Unity 3D.

 maps uses 3D perlin noise to assign a noise level to a 3D Grid representation.

Using a isolevel we compare to each vertex's noisevalue. we determine which verticies of the gridcell are considered terrain and which are considered air.
This algorithm "marches" through each cube (gridcell) calculating the mesh.

https://en.wikipedia.org/wiki/Marching_cubes
https://paulbourke.net/geometry/polygonise/
![marchingcubes](https://github.com/ACassiusD/marching-cubes-unity/assets/18119577/6d0725e5-5035-4815-b335-956d196c8343)

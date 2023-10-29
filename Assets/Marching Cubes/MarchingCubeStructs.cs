using System;
using UnityEngine;

// Triangle representation with 3 Vector3 points to represent the corners/vertices
// Will use these to create the terrain mesh
public struct Triangle
{
    public Vector3[] p;

    public Triangle(Vector3 p0, Vector3 p1, Vector3 p2)
    {
        p = new Vector3[3] { p0, p1, p2 };
    }
}

// Single Grid cell representation. With 8 Vector3 points to represent the 8 corners of the cube 
// And 8 float values to represent the noise values at each corner
// Also in the constructor ensures that both arrays have a length of 8
public struct GridCell
{
    public Vector3[] verticies;
    public float[] noiseValues;
    public GridCell(Vector3[] p, float[] val)
    {
        if (p.Length != 8 || val.Length != 8)
            throw new ArgumentException("Both arrays must have a length of 8.");

        this.verticies = p;
        this.noiseValues = val;
    }
}

using Godot;
using g3;
using System.Collections.Generic;
public partial class MarchingSquareGenerator : Node3D
{
  [Export] int width = 32;
  [Export] int height = 32;

  [Export] int noiseGain = 1;

  [Export] Image noiseTextureImage = null;

  [Export] float noiseThreshold = 0.01f;

  float[,] grid;

  public override void _Ready()
  {
    //Generate layers for each distinct height in the heightmap
    grid = new float[width, height];
    for (int x = 0; x < width; x++)
    {
      for (int y = 0; y < height; y++)
      {
        grid[x, y] = noiseTextureImage.GetPixel(x, y).R * noiseGain;
      }
    }
  }

  public DMesh3 MarchSquares(float[,] grid)
  {
    DMesh3 mesh = new DMesh3();
    for (int x = 0; x < grid.GetLength(0) - 1; x++)
    {
      for (int y = 0; y < grid.GetLength(1) - 1; y++)
      {

        float aVal = grid[x, y];
        float bVal = grid[x + 1, y];
        float cVal = grid[x + 1, y + 1];
        float dVal = grid[x, y + 1];

        var a = new Vector3(x, 0, y);
        var b = new Vector3(x + 1, 0, y);
        var c = new Vector3(x + 1, 0, y + 1);
        var d = new Vector3(x, 0, y + 1);



        var e = a + new Vector3(0.5f, 0, 0);
        var f = b + new Vector3(0, 0, 0.5f);
        var g = d + new Vector3(0.5f, 0, 0);
        var h = a + new Vector3(0, 0, 0.5f);

        /* Visualization of the cell
                    A--e--B
                    |     |
                    h     f
                    |     |
                    D--g--C
        */


        int cellType = (int)(aVal + bVal * 2 + cVal * 4 + dVal * 8);

        switch (cellType)
        {
          case 0:
            break;
          case 1:
            /*
            A--e--B
            | /   |
            h/    f
            |     |
            D--g--C
            */
            mesh.AddTriangle(a, e, h);
            break;
          case 2:
            /*
            A--e--B
            |   \ |
            h    \f
            |     |
            D--g--C
            */
            mesh.AddTriangle(b, f, e);
            break;
          case 3:
            /* Asteriks denote where the face the quad is
            A--e--B
            |* * *|
            h-----f
            |     |
            D--g--C
            */
            mesh.AddQuad(a, b, f, h);
            break;
          case 4:
            /* Visualization of the cell
                        A--e--B
                        |     |
                        h    /f
                        |   / |
                        D--g--C
            */
            mesh.AddTriangle(c, g, f);
            break;
          case 5:
            /* Visualization of the cell
                        A--e--B
                        | /   |
                        h/   /f
                        |   / |
                        D--g--C
            */
            mesh.AddTriangle(f, c, g);
            mesh.AddTriangle(h, a, e);
            break;
          case 6:
            //You probably get it now
            mesh.AddQuad(e, b, c, g);
            break;
          case 7:
            mesh.AddPentagon(a, b, c, g, h);
            break;
          case 8:
            mesh.AddTriangle(d, h, g);
            break;
          case 9:
            mesh.AddQuad(a, e, g, d);
            break;
          case 10:
            mesh.AddTriangle(g, d, h);
            mesh.AddTriangle(e, b, f);
            break;
          case 11:
            mesh.AddPentagon(a, b, f, g, d);
            break;
          case 12:
            mesh.AddQuad(h, f, c, d);
            break;
          case 13:
            mesh.AddPentagon(a, e, f, c, d);
            break;
          case 14:
            mesh.AddPentagon(h, e, b, c, d);
            break;
          case 15:
            mesh.AddQuad(a, b, c, d);
            break;
          default:
            GD.Print("Invalid cell type: " + cellType);
            break;
        }
      }
    }
    return mesh;
  }
}


public static class Extensions
{
  public static void AddQuad(this SurfaceTool st, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
  {
    st.AddTriangle(p1, p2, p4);
    st.AddTriangle(p2, p3, p4);
  }

  public static void AddTriangle(this SurfaceTool st, Vector3 p1, Vector3 p2, Vector3 p3)
  {
    //Calculate normal
    var normal = (p2 - p1).Cross(p3 - p1).Normalized();
    st.AddVertex(p1);
    st.AddVertex(p2);
    st.AddVertex(p3);
  }

  public static void AddPentagon(this SurfaceTool st, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5)
  {
    st.AddTriangle(p1, p2, p3);
    st.AddTriangle(p1, p3, p4);
    st.AddTriangle(p1, p4, p5);
  }

  public static void AddQuad(this DMesh3 pm, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
  {
    pm.AddTriangle(p1, p2, p4);
    pm.AddTriangle(p2, p3, p4);
  }

  public static void AddTriangle(this DMesh3 pm, Vector3 p1, Vector3 p2, Vector3 p3)
  {
    var a = new Vector3d(p1.X, p1.Y, p1.Z);
    var b = new Vector3d(p2.X, p2.Y, p2.Z);
    var c = new Vector3d(p3.X, p3.Y, p3.Z);
    var i = pm.AppendVertex(a);
    var j = pm.AppendVertex(b);
    var k = pm.AppendVertex(c);
    pm.AppendTriangle(i, j, k);
  }

  public static void AddPentagon(this DMesh3 pm, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Vector3 p5)
  {
    pm.AddTriangle(p1, p2, p3);
    pm.AddTriangle(p1, p3, p4);
    pm.AddTriangle(p1, p4, p5);
  }

  public static ArrayMesh ToGodotMesh(this DMesh3 mesh)
  {
    var st = new SurfaceTool();
    st.Begin(Mesh.PrimitiveType.Triangles);
    for (int i = 0; i < mesh.VertexCount; i++)
    {
      Vector3 p = new Vector3((float)mesh.GetVertex(i).x, (float)mesh.GetVertex(i).y, (float)mesh.GetVertex(i).z);
      st.SetSmoothGroup(uint.MaxValue);
      st.AddVertex(p);
    }
    st.GenerateNormals();

    return st.Commit();
  }
}
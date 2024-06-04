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

  Dictionary<float, float[,]> grids = new Dictionary<float, float[,]>();

  public override void _Ready()
  {
    //Generate layers for each distinct height in the heightmap
    for (int x = 0; x < noiseTextureImage.GetWidth(); x++)
    {
      for (int y = 0; y < noiseTextureImage.GetHeight(); y++)
      {
        float fn = noiseTextureImage.GetPixel(x, y).R;

        if (!grids.ContainsKey(fn))
        {
          grids.Add(fn, new float[width, height]);
        }
      }
    }

    //Generate data from the heightmap
    foreach (KeyValuePair<float, float[,]> kvp in grids)
    {
      float[,] grid = kvp.Value;
      for (int x = 0; x < width; x++)
      {
        for (int y = 0; y < height; y++)
        {
          //Map from the heightmap to the grid
          float fx = x / (float)noiseTextureImage.GetWidth();
          float fy = y / (float)noiseTextureImage.GetHeight();
          float fn = noiseTextureImage.GetPixel((int)(fx * noiseTextureImage.GetWidth()), (int)(fy * noiseTextureImage.GetHeight())).R;
          if (fn == kvp.Key)
            grid[x, y] = 1.0f;
        }
      }
    }

    foreach (KeyValuePair<float, float[,]> kvp in grids)
    {
      MeshInstance3D mesh = new MeshInstance3D();
      mesh.Mesh = MarchSquares(kvp.Value).ToGodotMesh();
      mesh.Position = new Vector3(-width / 2, kvp.Key, -height / 2);
      AddChild(mesh);
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

        /*
        A--e--B
        |     |
        h     f
        |     |
        D--g--C
        */


        var e = a + new Vector3(0.5f, 0, 0);
        var f = b + new Vector3(0, 0, 0.5f);
        var g = d + new Vector3(0.5f, 0, 0);
        var h = a + new Vector3(0, 0, 0.5f);

        int cellType = (int)(aVal + bVal * 2 + cVal * 4 + dVal * 8);
        switch (cellType)
        {
          case 0:
            break;
          case 1:
            mesh.AddTriangle(a, e, h);
            break;
          case 2:
            mesh.AddTriangle(b, f, e);
            break;
          case 3:
            mesh.AddQuad(a, b, f, h);
            break;
          case 4:
            mesh.AddTriangle(c, g, f);
            break;
          case 5:
            mesh.AddTriangle(f, c, g);
            mesh.AddTriangle(h, a, e);
            break;
          case 6:
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
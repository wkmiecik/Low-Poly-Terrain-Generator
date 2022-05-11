using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;


public class RiverSpawner : MonoBehaviour
{
    [HideInInspector] public static int xsize = 300;
    [HideInInspector] public static int ysize = 300;
    [HideInInspector] public static TriangleNet.Mesh mesh;
    [HideInInspector] public static List<Vertex> edgeVertices;
    [HideInInspector] public static List<float> elevations;

    public List<TriangleNode> triangleGraph = new List<TriangleNode>();

    public int seed = 0;
    public int showVertexIndex = 0;

    RandomNumbers rng;

    public class TriangleNode
    {
        public Triangle triangle;
        public Vector3 center
        {
            get
            {
                Vector3 sum = new Vector3(0, 0, 0);
                foreach (var vert in triangle.vertices)
                {
                    if (vert != null)
                    {
                        sum += new Vector3((float)vert.x, (float)elevations[vert.id], (float)vert.y);
                    }
                }
                return sum / triangle.vertices.Length;
            }
        }

        public List<TriangleNode> neighbors
        {
            get
            {
                var neighbors = new List<TriangleNode>();
                foreach (var neighbor in triangle.neighbors)
                {
                    if (neighbor.Triangle.vertices[0] != null)
                    {
                        neighbors.Add(new TriangleNode(neighbor.Triangle));
                    }
                }
                return neighbors;
            }
        }


        public TriangleNode(Triangle triangle)
        {
            this.triangle = triangle;
        }
    }

    public void Generate()
    {
        rng = new RandomNumbers(seed);

        var edgeLen = edgeVertices.Count;

        edgeVertices.Sort((Vertex v1, Vertex v2) =>
        {
            if (elevations[v1.id] > elevations[v2.id]) return -1;
            else if (elevations[v1.id] < elevations[v2.id]) return 1;
            else return 0;
        });


        // Choose one of 2/5 top vertices
        int riverTopIndex = Mathf.FloorToInt(rng.Range(0f, (edgeLen * 2) / 5));

        // Choose one of 1/5 bottom vertices on different edge than top vertex
        int riverBottomIndex = -1;
        for (int i = 0; i < 300; i++)
        {
            int temp = Mathf.FloorToInt(rng.Range(edgeLen - (edgeLen / 5), edgeLen - 1));

            if (edgeVertices[temp].x == 0 && edgeVertices[riverTopIndex].x == xsize)
                riverBottomIndex = temp;
            if (edgeVertices[temp].x == xsize && edgeVertices[riverTopIndex].x == 0)
                riverBottomIndex = temp;
            if (edgeVertices[temp].y == 0 && edgeVertices[riverTopIndex].y == ysize)
                riverBottomIndex = temp;
            if (edgeVertices[temp].y == ysize && edgeVertices[riverTopIndex].y == 0)
                riverBottomIndex = temp;

            if (riverBottomIndex != -1) break;
        }
        if (riverBottomIndex == -1)
        {
            Debug.Log("Could not find correct point for river ends");
            return;
        }

        // Change edge indexes to mesh indexes
        riverTopIndex = edgeVertices[riverTopIndex].id;
        riverBottomIndex = edgeVertices[riverBottomIndex].id;

        // Get triangles from mesh and find edge top/bottom triangles
        IEnumerator<Triangle> triangleEnumerator = mesh.Triangles.GetEnumerator();
        List<Triangle> triangles = new List<Triangle>();
        triangleEnumerator.MoveNext();
        int topTriangleIndex = -1;
        int bottomTriangleIndex = -1;
        do
        {
            triangles.Add(triangleEnumerator.Current);

            if(triangleEnumerator.Current.Contains(mesh.vertices[riverTopIndex].x, mesh.vertices[riverTopIndex].y))
                topTriangleIndex = triangleEnumerator.Current.id;
            if (triangleEnumerator.Current.Contains(mesh.vertices[riverBottomIndex].x, mesh.vertices[riverBottomIndex].y))
                bottomTriangleIndex = triangleEnumerator.Current.id;

        } while (triangleEnumerator.MoveNext());

        // Create triangle 'graph' structure
        triangleGraph = new List<TriangleNode>();
        for (int i = 0; i < triangles.Count; i++)
        {
            triangleGraph.Add(new TriangleNode(triangles[i]));
        }


        DrawVertex(triangleGraph[topTriangleIndex].center, Color.black);
        foreach (var item in triangleGraph[topTriangleIndex].neighbors)
        {
            DrawVertex(item.center, Color.red);
        }

        DrawVertex(triangleGraph[bottomTriangleIndex].center, Color.green);
        foreach (var item in triangleGraph[bottomTriangleIndex].neighbors)
        {
            DrawVertex(item.center, Color.blue);
        }
    }

    // DEBUGGING
    private void Update()
    {
        if (edgeVertices != null)
        {
            Generate();
        }
    }



    public Vector3 getTriangleCenter(Triangle tri)
    {
        Vector3 sum = new Vector3(0, 0, 0);

        foreach (var vert in tri.vertices)
        {
            if (vert != null)
            {
                sum += new Vector3((float)vert.x, (float)elevations[vert.id], (float)vert.y);
            }
        }

        return sum / tri.vertices.Length;
    }

    void DrawVertex(Vertex vertex, Color color)
    {
        var pos = new Vector3((float)vertex.x, elevations[vertex.id], (float)vertex.y);
        Debug.DrawLine(pos, pos + Vector3.up * 20, color);
    }

    void DrawVertex(Vector3 vertex, Color color)
    {
        var pos = vertex;
        Debug.DrawLine(pos, pos + Vector3.up * 20, color);
    }
}

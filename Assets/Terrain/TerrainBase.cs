using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;

public class TerrainBase : MonoBehaviour
{
    public Transform baseChunkPrefab = null;
    [HideInInspector] public List<float> elevations;

    public void MakeBase(List<Vertex> edgeVertices, int xsize, int ysize)
    {
        Polygon polygon = new Polygon();

        for (int i = 0; i < edgeVertices.Count; i++)
        {
            if (edgeVertices[i].x == xsize)
            {
                var v = new Vertex(elevations[edgeVertices[i].id], edgeVertices[i].y, 1);
                polygon.Add(v);
            }
        }

        polygon.Points.Sort(delegate (Vertex v1, Vertex v2)
        {
            if (v1.y > v2.y) return -1;
            else if (v1.y < v2.y) return 1;
            else if (v1.x > v2.x) return 1;
            else if (v1.x < v2.x) return -1;
            else return 0;
        });
        polygon.Add(new Vertex(-100, 0, 1));
        polygon.Add(new Vertex(-100, xsize, 1));

        for (int i = 0; i < polygon.Points.Count - 1; i++)
        {
            polygon.Segments.Add(new Segment(polygon.Points[i], polygon.Points[i + 1]));
        }
        polygon.Segments.Add(new Segment(polygon.Points[0], polygon.Points[polygon.Points.Count - 1]));

        var xPlusBase = MakeMeshFromPolygon(polygon, new Vector3(xsize, 0, 0), Quaternion.Euler(0, 0, 90));
    }

    private Transform MakeMeshFromPolygon(Polygon polygon, Vector3 pos, Quaternion rot)
    {
        var options = new ConstraintOptions() { ConformingDelaunay = false, Convex = false, SegmentSplitting = 0 };
        TriangleNet.Mesh baseMesh = (TriangleNet.Mesh)polygon.Triangulate(options);

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        IEnumerator<Triangle> triangleEnumerator = baseMesh.Triangles.GetEnumerator();

        for (int i = 0; i < baseMesh.triangles.Count; i++)
        {
            if (!triangleEnumerator.MoveNext())
            {
                break;
            }

            Triangle triangle = triangleEnumerator.Current;

            Vector3 v0 = new Vector3((float)triangle.vertices[0].x, 0, (float)triangle.vertices[0].y);
            Vector3 v1 = new Vector3((float)triangle.vertices[1].x, 0, (float)triangle.vertices[1].y);
            Vector3 v2 = new Vector3((float)triangle.vertices[2].x, 0, (float)triangle.vertices[2].y);

            triangles.Add(vertices.Count);
            triangles.Add(vertices.Count + 1);
            triangles.Add(vertices.Count + 2);

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));
            uvs.Add(new Vector2(0.0f, 0.0f));
        }
        Mesh chunkMesh = new Mesh();
        chunkMesh.vertices = vertices.ToArray();
        chunkMesh.uv = uvs.ToArray();
        chunkMesh.triangles = triangles.ToArray();
        chunkMesh.normals = normals.ToArray();

        Transform chunk = Instantiate<Transform>(baseChunkPrefab, pos, rot);
        chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
        chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        chunk.transform.parent = transform;
        return chunk;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;
using PathCreation;

public class RiverSpawner : MonoBehaviour
{
    [HideInInspector] public static int xsize = 300;
    [HideInInspector] public static int ysize = 300;
    [HideInInspector] public static TriangleNet.Mesh mesh;
    [HideInInspector] public static List<Vertex> edgeVertices;
    [HideInInspector] public static List<float> elevations;
    [HideInInspector] public static float riverHeightOffset = -10;

    public List<TriangleNode> triangleGraph = new List<TriangleNode>();

    public int seed = 0;
    public int gCostMultiplier = 1;
    public int hCostMultiplier = 1;

    public PathMeshCreator pathMeshCreator;
    [HideInInspector] public PathCreator pathCreator;

    RandomNumbers rng;
    public bool update;

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
                        sum += new Vector3((float)vert.x, (float)elevations[vert.id] + riverHeightOffset, (float)vert.y);
                    }
                }
                return (sum / triangle.vertices.Length);
            }
        }
        public List<int> neighbors
        {
            get
            {
                var neighbors = new List<int>();
                foreach (var neighbor in triangle.neighbors)
                {
                    if (neighbor.Triangle.vertices[0] != null)
                    {
                        neighbors.Add(neighbor.Triangle.id);
                    }
                }
                return neighbors;
            }
        }

        public float height => center.y;

        public int parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;


        public TriangleNode(Triangle triangle)
        {
            this.triangle = triangle;
        }
    }


    private void Start()
    {
        pathCreator = gameObject.GetComponent<PathCreator>();
        pathMeshCreator = gameObject.GetComponent<PathMeshCreator>();
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
        int riverTopIndex = Mathf.FloorToInt(rng.Range(0f, edgeLen / 5));

        // Choose one of 1/5 bottom vertices on different edge than top vertex
        int riverBottomIndex = -1;
        for (int i = 0; i < 300; i++)
        {
            int temp = Mathf.FloorToInt(rng.Range(edgeLen - (edgeLen / 10), edgeLen - 1));

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
            pathMeshCreator.gameObject.SetActive(false);
            return;
        } else
        {
            pathMeshCreator.gameObject.SetActive(true);
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



        // Pathfinding
        int searchIterationLimit = 10000;
        int searchIterations = 0;
        bool pathfindingSuccess = false;

        var openList = new List<int>();
        var closedList = new Dictionary<int, int>();

        int previousNode = topTriangleIndex;
        openList.Add(topTriangleIndex);

        while (openList.Count != 0)
        {
            searchIterations++;
            if (searchIterations >= searchIterationLimit)
            {
                Debug.LogWarning("Path iteration limit exceeded");
                break;
            }

            // Get current node from open list
            int currentNode = openList[0];
            foreach (var node in openList)
            {
                if (triangleGraph[node].fCost < triangleGraph[currentNode].fCost || triangleGraph[node].fCost == triangleGraph[currentNode].fCost && triangleGraph[node].hCost < triangleGraph[currentNode].hCost)
                    currentNode = node;
            }

            // Remove current node from open list and add it to close list
            openList.Remove(currentNode);
            closedList.Add(currentNode, previousNode);

            if (currentNode == bottomTriangleIndex)
            {
                previousNode = currentNode;
                pathfindingSuccess = true;
            }
            if (pathfindingSuccess) break;

            // Find neighbors of current node and add them to open list
            foreach (var neighbor in triangleGraph[currentNode].neighbors)
            {
                if (!closedList.ContainsKey(neighbor))
                {
                    int thisHeight = Mathf.RoundToInt(triangleGraph[currentNode].height * 10);
                    int neighborHeight = Mathf.RoundToInt(triangleGraph[neighbor].height * 10);
                    int movementCost = triangleGraph[currentNode].gCost + (neighborHeight - thisHeight);
                    if (movementCost < triangleGraph[neighbor].gCost || !openList.Contains(neighbor))
                    {
                        triangleGraph[neighbor].gCost = movementCost * gCostMultiplier;
                        triangleGraph[neighbor].gCost += Mathf.RoundToInt(Vector3.Distance(triangleGraph[neighbor].center, new Vector3(xsize / 2, -20, ysize / 2)) * 0.05f);

                        triangleGraph[neighbor].hCost = Mathf.RoundToInt(Vector3.Distance(triangleGraph[neighbor].center, triangleGraph[bottomTriangleIndex].center) * hCostMultiplier);
                        triangleGraph[neighbor].parent = currentNode;

                        if (!openList.Contains(neighbor)) openList.Add(neighbor);
                    }
                }
            }
            previousNode = currentNode;
        }

        List<Vector3> path = new List<Vector3>();
        int currentTraceNode = bottomTriangleIndex;
        while (currentTraceNode != topTriangleIndex)
        {
            path.Add(triangleGraph[currentTraceNode].center);

            for (int i = 0; i < 20; i++)
            {
                currentTraceNode = triangleGraph[currentTraceNode].parent;
                if (currentTraceNode == topTriangleIndex) 
                {
                    path.Add(triangleGraph[currentTraceNode].center);
                    break;
                }
            }
        }
        path.Reverse();


        // Path cleanup
        if (Vector3.Distance(path[0], path[1]) < 25f) path.RemoveRange(1, 1);
        for (int i = 0; i < path.Count; i++)
        {
            if (i == 0 || i == path.Count - 1)
            {
                if (Mathf.Abs(path[i].x - xsize) < 7) path[i] = new Vector3(xsize, path[i].y, path[i].z);
                if (Mathf.Abs(path[i].x) < 7) path[i] = new Vector3(0, path[i].y, path[i].z);
                if (Mathf.Abs(path[i].z - ysize) < 7) path[i] = new Vector3(path[i].x, path[i].y, ysize);
                if (Mathf.Abs(path[i].z) < 7) path[i] = new Vector3(path[i].x, path[i].y, 0);
            }
            DrawVertex(path[i], Color.red);
        }


        // Path drawing
        pathCreator.bezierPath = new BezierPath(path);
        pathCreator.bezierPath.ControlPointMode = BezierPath.ControlMode.Aligned;


        float handleDistanceFromEdge = 20;
        if (path[0].x == xsize)
        {
            pathCreator.bezierPath.MovePoint(1, new Vector3(path[0].x - handleDistanceFromEdge, (path[0].y + path[1].y) / 1.5f, path[0].z), true);
            pathCreator.bezierPath.MovePoint(pathCreator.bezierPath.NumPoints - 2, new Vector3(handleDistanceFromEdge, (path[path.Count - 1].y + path[path.Count - 2].y) / 2, path[path.Count - 1].z), true);
        }
        if (path[0].x == 0)
        {
            pathCreator.bezierPath.MovePoint(1, new Vector3(handleDistanceFromEdge, (path[0].y + path[1].y) / 1.5f, path[0].z), true);
            pathCreator.bezierPath.MovePoint(pathCreator.bezierPath.NumPoints - 2, new Vector3(path[path.Count - 1].x - handleDistanceFromEdge, (path[path.Count - 1].y + path[path.Count - 2].y) / 2, path[path.Count - 1].z), true);
        }
        if (path[0].z == ysize)
        {
            pathCreator.bezierPath.MovePoint(1, new Vector3(path[0].x, (path[0].y + path[1].y) / 1.5f, path[0].z - handleDistanceFromEdge), true);
            pathCreator.bezierPath.MovePoint(pathCreator.bezierPath.NumPoints - 2, new Vector3(path[path.Count - 1].x, (path[path.Count - 1].y + path[path.Count - 2].y) / 2, handleDistanceFromEdge), true);
        }
        if (path[0].z == 0)
        {
            pathCreator.bezierPath.MovePoint(1, new Vector3(path[0].x, (path[0].y + path[1].y) / 1.5f, handleDistanceFromEdge), true);
            pathCreator.bezierPath.MovePoint(pathCreator.bezierPath.NumPoints - 2, new Vector3(path[path.Count - 1].x, (path[path.Count - 1].y + path[path.Count - 2].y) / 2, path[path.Count - 1].z - handleDistanceFromEdge), true);
        }


        pathCreator.bezierPath.NotifyPathModified();
        pathMeshCreator.pathCreator = pathCreator;
        pathMeshCreator.UpdateMesh();
    }

    // DEBUGGING
    private void Update()
    {
        //if (edgeVertices != null && update)
        //{
        //    Generate();
        //}
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

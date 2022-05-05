﻿using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;

public class DelaunayTerrain : MonoBehaviour {
    [Header("Terrain")]
    public int seed = 0;
    float[] seeds;

    public int xsize = 300;
    public int ysize = 300;

    public float minPointRadius = 12f;

    public int randomPoints = 0;

    public int trianglesInChunk = 20000;

    public float elevationScale = 170.0f;
    public float sampleSize = 1.0f;
    public int octaves = 8;
    public float frequencyBase = 1.4f;
    public float persistence = 1.23f;

    public Transform chunkPrefab = null;

    private List<float> elevations;

    [HideInInspector] public static TriangleNet.Mesh mesh = null;

    public float vertexMergeSize = 0;
    public float vertexEdgeMergeDistance = 0;

    [SerializeField] bool regenerate = false;

    private static List<Vertex> edgeVertices;
    private TerrainBase terrainBase;

    [Header("Base")]
    public float topLayerSize = 9;
    public float bottomLayerSize = 60;

    [Header("Road")]
    public RoadMeshCreator roadMeshCreator;
    public float roadSmoothDistance = 70;
    public float roadHeightSmoothDistance = 20f;

    [Header("Trees")]
    public float treeMinPointRadius = 18;
    public float treeDistanceFromEdges = 6;
    private TreesSpawner treesSpawner;

    [Header("Rocks")]
    public float rockMinPointRadius = 47;
    public float rockDistanceFromEdges = 6;
    private RocksSpawner rocksSpawner;

    [Header("House")]
    public float houseDistanceFromPath = 45;
    public int houseDistanceFromEdge = 35;
    private HouseSpawner house;


    [Header("Generation steps")]
    public bool generateBase = true;
    public bool generateRoad = true;
    public bool generateHouse = true;
    public bool generateRocks = true;
    public bool generateTrees = true;

    private List<GameObject> toDelete = new List<GameObject>();

    private void Start()
    {
        terrainBase = GetComponentInChildren<TerrainBase>();
        treesSpawner = GetComponentInChildren<TreesSpawner>();
        rocksSpawner = GetComponentInChildren<RocksSpawner>();
        house = GetComponentInChildren<HouseSpawner>();

        Generate();
    }

    void Update() 
    {
        if (regenerate) 
        {
            regenerate = false;

            // Clear playing animations
            StopAllCoroutines();
            DOTween.Clear();

            // Delete everything on delete list
            foreach (var gameObject in toDelete) 
            {
                Destroy(gameObject);
            }
            toDelete.Clear();

            // Reset road
            var firstPoint = new Vector2(150, ysize - 0.015f);
            var middlePoint = new Vector2(xsize / 2 + 7.5f, ysize / 2 + 7.5f);
            var lastPoint = new Vector2(150, 0.015f);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(firstPoint.x, 0, firstPoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(lastPoint.x, 0, lastPoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, new Vector3(150, 0, 230), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(middlePoint.x, 0, middlePoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();
            roadMeshCreator.UpdateMesh();

            Generate();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            regenerate = true;
        }
    }

    public virtual void Generate() 
    {
        var rng = new RandomNumbers(seed);

        Polygon polygon = new Polygon();

        elevations = new List<float>();
        seeds = new float[octaves];
        for (int i = 0; i < octaves; i++) 
        {
            seeds[i] = rng.Range(0.0f, 100.0f);
        }


        // Randomise path
        var pointsToAvoid = new List<Vector3>();
        if (generateRoad)
        {
            var firstPoint = new Vector2(rng.Range(30, xsize - 30), ysize - 0.015f);
            var middlePoint = new Vector2(xsize / 2 + rng.Range(-15, 15), ysize / 2 + rng.Range(-15, 15));
            var lastPoint = new Vector2(rng.Range(30, xsize - 30), 0.015f);

            roadMeshCreator.gameObject.SetActive(true);
            // Edge point
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(firstPoint.x, GetPerlinElevation(firstPoint, roadHeightSmoothDistance), firstPoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(lastPoint.x, GetPerlinElevation(lastPoint, roadHeightSmoothDistance), lastPoint.y), true);
            // Middle point and its handles
            var handle = new Vector3(rng.Range(10, xsize - 10), 0, rng.Range((ysize / 2) + 40, ysize - 20));
            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, handle, true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(middlePoint.x, GetPerlinElevation(middlePoint, roadHeightSmoothDistance), middlePoint.y), true);

            // Update road mesh
            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();
            roadMeshCreator.UpdateMesh();

            // Generate points spaced along path
            pointsToAvoid = roadMeshCreator.pathCreator.path.GeneratePointsAlongPath(6);
        } 
        else
        {
            roadMeshCreator.gameObject.SetActive(false);
        }

        // Spawn house
        if (generateHouse)
        {
            var houseObj = house.Generate(xsize, ysize, pointsToAvoid, houseDistanceFromPath, houseDistanceFromEdge, seed);
            if (houseObj != null) toDelete.Add(houseObj);
        }

        // Spawn trees
        if (generateTrees)
        {
            StartCoroutine(treesSpawner.Generate(
                xsize,
                ysize,
                treeMinPointRadius,
                treeDistanceFromEdges,
                pointsToAvoid,
                roadMeshCreator.roadWidth + minPointRadius + 6,
                toDelete,
                seed));
        }

        // Spawn rocks
        if (generateRocks)
        {
            StartCoroutine(rocksSpawner.Generate(
                xsize,
                ysize,
                rockMinPointRadius,
                rockDistanceFromEdges,
                pointsToAvoid,
                roadMeshCreator.roadWidth + 4,
                toDelete,
                seed));
        }


        //Add uniformly-spaced points
        foreach (Vector2 sample in PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize, ysize)))
        {
            polygon.Add(new Vertex((double)sample.x, (double)sample.y));
        }

        // Add some randomly sampled points
        for (int i = 0; i < randomPoints; i++) 
        {
            polygon.Add(new Vertex(rng.Range(0.0f, xsize), rng.Range(0.0f, ysize)));
        }
        // Add corner points
        polygon.Add(new Vertex(0, 0, 1));
        polygon.Add(new Vertex(0, ysize, 1));
        polygon.Add(new Vertex(xsize, 0, 1));
        polygon.Add(new Vertex(xsize, ysize, 1));

        ConstraintOptions options = new ConstraintOptions() { ConformingDelaunay = true };
        mesh = (TriangleNet.Mesh)polygon.Triangulate(options);

        // Generating height at every point
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            // Base height from perlin noise
            float elevation = GetPerlinElevation(new Vector2((float)mesh.vertices[i].x, (float)mesh.vertices[i].y));

            //var point = roadMeshCreator.pathCreator.path.GetClosestPointOnPath(mesh.vertices[i]);
            // Fix height along path
            // Possible optimization: save 2 closest point and calculate average height from them. Could then decrease amount of points on road.
            var roadWidth = roadMeshCreator.roadWidth + minPointRadius + 6;
            if (pointsToAvoid.Count > 0)
            {
                Vector3 closestPointOnRoad = Vector3.zero;
                float distToRoad = float.MaxValue;
                foreach (var point in pointsToAvoid)
                {
                    var Point2Da = new Vector2(point.x, point.z);
                    var Point2Db = new Vector2((float)mesh.vertices[i].x, (float)mesh.vertices[i].y);
                    var dist = Vector2.Distance(Point2Da, Point2Db);

                    if (dist < distToRoad)
                    {
                        distToRoad = dist;
                        closestPointOnRoad = point;
                    }
                }

                if (distToRoad <= roadWidth)
                {
                    elevation = closestPointOnRoad.y - 1;
                }
                else if (distToRoad > roadWidth && distToRoad < roadSmoothDistance)
                {
                    elevation = map(distToRoad, roadWidth, roadSmoothDistance, closestPointOnRoad.y - 1, elevation);
                }
            }

            elevations.Add(elevation);
        }
        //foreach (var item in pointsToAvoid)
        //{
        //    Debug.DrawLine(item, item + Vector3.up * 20, Color.red, 15);
        //}


        // Collect all vertices on the edge of terrain for generatig base
        edgeVertices = new List<Vertex>();
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            if (mesh.vertices[i].label == 1)
            {
                edgeVertices.Add(mesh.vertices[i]);
            }
        }


        // Make terrain mesh
        MakeMesh();

        // Make base meshes
        if (generateBase)
        {
            terrainBase.elevations = elevations;
            terrainBase.xsize = xsize;
            terrainBase.ysize = ysize;
            terrainBase.topLayerSize = topLayerSize;
            terrainBase.bottomLayerSize = bottomLayerSize;
            toDelete.AddRange(terrainBase.MakeBase(edgeVertices));
        }
    }


    public float GetPerlinElevation(Vector2 point, float averageDist = 0)
    {
        if (averageDist > 0)
        {
            float sum = 0;
            sum += GetPerlinElevation(point);
            sum += GetPerlinElevation(point + Vector2.left * averageDist);
            sum += GetPerlinElevation(point + Vector2.right * averageDist);
            sum += GetPerlinElevation(point + Vector2.up * averageDist);
            sum += GetPerlinElevation(point + Vector2.down * averageDist);

            sum += GetPerlinElevation(point + ((Vector2.left + Vector2.up) / 2) * averageDist);
            sum += GetPerlinElevation(point + ((Vector2.right + Vector2.up) / 2) * averageDist);
            sum += GetPerlinElevation(point + ((Vector2.left + Vector2.down) / 2) * averageDist);
            sum += GetPerlinElevation(point + ((Vector2.right + Vector2.down) / 2) * averageDist);

            return sum / 9;
        }

        // Base height from perlin noise
        float elevation = 0.0f;
        float amplitude = Mathf.Pow(persistence, octaves);
        float frequency = 1.0f;
        float maxVal = 0.0f;

        for (int o = 0; o < octaves; o++)
        {
            float sample = (Mathf.PerlinNoise(seeds[o] + point.x * sampleSize / xsize * frequency,
                                              seeds[o] + point.y * sampleSize / ysize * frequency) - 0.5f) * amplitude;
            elevation += sample;
            maxVal += amplitude;
            amplitude /= persistence;
            frequency *= frequencyBase;
        }

        elevation = (elevation / maxVal) * elevationScale;

        return elevation;
    }

    public void MakeMesh() 
    {
        IEnumerator<Triangle> triangleEnumerator = mesh.Triangles.GetEnumerator();

        for (int chunkStart = 0; chunkStart < mesh.Triangles.Count; chunkStart += trianglesInChunk) 
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            int chunkEnd = chunkStart + trianglesInChunk;
            for (int i = chunkStart; i < chunkEnd; i++) {
                if (!triangleEnumerator.MoveNext()) {
                    break;
                }

                Triangle triangle = triangleEnumerator.Current;

                Vector3 v0 = GetPoint3D(triangle.vertices[2].id);
                Vector3 v1 = GetPoint3D(triangle.vertices[1].id);
                Vector3 v2 = GetPoint3D(triangle.vertices[0].id);

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

            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation, transform);
            toDelete.Add(chunk.gameObject);
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
        }
    }

    public Vector3 GetPoint3D(int index) 
    {
        Vertex vertex = mesh.vertices[index];
        float elevation = elevations[index];
        return new Vector3((float)vertex.x, elevation, (float)vertex.y);
    }

    float map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }

}
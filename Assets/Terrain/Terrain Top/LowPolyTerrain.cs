﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;

public class LowPolyTerrain : MonoBehaviour {
    [Header("Terrain")]
    public int seed = 0;
    float[] seeds;

    public int size = 300;
    private int xsize = 300;
    private int ysize = 300;

    public float minPointRadius = 12f;

    public int randomPoints = 30;

    public int trianglesInChunk = 20000;

    public float elevationScale = 250f;
    public int octaves = 9;
    public float noiseFrequency = 49f;
    private float frequencyBase;
    public float persistence = 0.5f;

    public Transform chunkPrefab = null;

    private List<float> elevations;

    [HideInInspector] public TriangleNet.Mesh mesh = null;

    private static List<Vertex> edgeVertices;
    private TerrainBase terrainBase;

    [Header("Base")]
    public float topLayerSize = 13;
    public float bottomLayerSize = 120;

    [Header("Road")]
    public PathMeshCreator roadMeshCreator;
    public float roadSmoothDistance = 50;
    public float roadHeightSmoothDistance = 20f;
    [Range(0, 1)]
    public float roadFill = 1f;

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

    [Header("River")]
    public float riverFlatDistance = 5;
    public float riverSmoothDistance = 30;
    public float riverHeightOffset = -10;
    private RiverSpawner riverSpawner;


    [Header("Generation steps")]
    public bool generateBase = true;
    public bool generateRocks = true;
    public bool generateTrees = true;
    public bool generateRoad = true;
    public bool generateHouse = true;
    public bool generateRiver = true;

    [Header("Generation animations")]
    public bool baseAnimation = true;
    public bool rocksAnimation = true;
    public bool treesAnimation = true;

    public bool roadAnimation = true;
    private bool roadAnimationPlaying = false;

    public bool houseAnimation = true;
    

    [Header("Update")]
    public bool regenerate = false;

    // Regenerating
    private List<GameObject> toDeleteList = new List<GameObject>();

    private void Start()
    {
        terrainBase = GetComponentInChildren<TerrainBase>();
        treesSpawner = GetComponentInChildren<TreesSpawner>();
        rocksSpawner = GetComponentInChildren<RocksSpawner>();
        house = GetComponentInChildren<HouseSpawner>();
        riverSpawner = GetComponentInChildren<RiverSpawner>();

        regenerate = true;
    }

    void FixedUpdate() 
    {
        if (regenerate) 
        {
            regenerate = false;

            // Clear playing animations
            StopAllCoroutines();
            DOTween.Clear();

            // Delete everything on delete list
            foreach (var gameObject in toDeleteList) 
            {
                Destroy(gameObject);
            }
            toDeleteList.Clear();

            // Reset road
            roadMeshCreator.pathCreator.bezierPath = new PathCreation.BezierPath(new Vector3(xsize / 2, 0, ysize / 2));
            roadMeshCreator.pathCreator.bezierPath.AddSegmentToStart(Vector3.zero);

            var firstPoint = new Vector2(150, ysize - 0.015f);
            var middlePoint = new Vector2(xsize / 2, ysize / 2);
            var lastPoint = new Vector2(150, 0.015f);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(firstPoint.x, 0, firstPoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(lastPoint.x, 0, lastPoint.y), true);

            roadMeshCreator.pathCreator.bezierPath.MovePoint(1, new Vector3(firstPoint.x, 0, firstPoint.y - 50), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(5, new Vector3(lastPoint.x, 0, lastPoint.y + 50), true);

            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, new Vector3(100, 0, 230), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(middlePoint.x, 0, middlePoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(4, new Vector3(200, 0, 70), true);

            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();

            StartCoroutine(Generate());
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            // Set variables
            xsize = size;
            ysize = size;
            frequencyBase = noiseFrequency / 100;

            regenerate = true;
        }
    }

    public IEnumerator Generate() 
    {
        yield return new WaitForFixedUpdate();

        var rng = new RandomNumbers(seed);

        edgeVertices = new List<Vertex>();
        var polygon = new Polygon();

        elevations = new List<float>();
        seeds = new float[octaves];
        for (int i = 0; i < octaves; i++) 
        {
            seeds[i] = rng.Range(0.0f, 100.0f);
        }


        //Add uniformly-spaced points
        foreach (Vector2 sample in PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize, ysize), seed: seed))
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
        // Triangulate polygon
        ConstraintOptions options = new ConstraintOptions() { ConformingDelaunay = true };
        mesh = (TriangleNet.Mesh)polygon.Triangulate(options);
        // Generate heights
        GenerateHeightsForMesh();




        // Generate path
        roadMeshCreator.gameObject.SetActive(generateRoad);
        var pointsToAvoid = new List<Vector3>();
        if (generateRoad)
        {
            if (roadAnimation)
            {
                roadFill = 0.01f;
                roadAnimationPlaying = true;
                roadAnimation = false;
            }

            var firstPoint = new Vector2(rng.Range(30, xsize - 30), ysize - 0.015f);
            var middlePoint = new Vector2(xsize / 2 + rng.Range(-15, 15), ysize / 2 + rng.Range(-15, 15));
            var lastPoint = new Vector2(rng.Range(30, xsize - 30), 0.015f);

            // Edge point
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(firstPoint.x, GetPerlinElevation(firstPoint, roadHeightSmoothDistance), firstPoint.y), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(lastPoint.x, GetPerlinElevation(lastPoint, roadHeightSmoothDistance), lastPoint.y), true);
            // Middle point and its handles
            var handle = new Vector3(rng.Range(10, xsize - 10), 0, rng.Range((ysize / 2) + 40, ysize - 20));
            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, handle, true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(middlePoint.x, GetPerlinElevation(middlePoint, roadHeightSmoothDistance), middlePoint.y), true);

            // Add new segments for the path so it looks more accurate
            var newAnchor1 = roadMeshCreator.pathCreator.bezierPath.SplitSegment(Vector3.zero, 0, 0.5f, false);
            var newAnchor2 = roadMeshCreator.pathCreator.bezierPath.SplitSegment(Vector3.zero, 2, 0.5f, false);
            var newAnchor1pos = roadMeshCreator.pathCreator.bezierPath.GetPoint(newAnchor1);
            var newAnchor2pos = roadMeshCreator.pathCreator.bezierPath.GetPoint(newAnchor2);
            newAnchor1pos.y = GetPerlinElevation(new Vector2(newAnchor1pos.x, newAnchor1pos.z), roadHeightSmoothDistance);
            newAnchor2pos.y = GetPerlinElevation(new Vector2(newAnchor2pos.x, newAnchor2pos.z), roadHeightSmoothDistance);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(newAnchor1, newAnchor1pos, true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(newAnchor2, newAnchor2pos, true);

            // Update road path
            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();

            // Create road mesh
            roadMeshCreator.UpdateMesh(roadFill);

            // Generate points spaced along path
            pointsToAvoid = roadMeshCreator.pathCreator.path.GeneratePointsAlongPath(6);
            pointsToAvoid = pointsToAvoid.GetRange(0, Mathf.CeilToInt(pointsToAvoid.Count * Mathf.Clamp01(roadFill + 0.25f)));

            if (roadAnimationPlaying)
            {
                roadFill += Time.deltaTime * 0.9f;
                roadFill = Mathf.Clamp01(roadFill);
                if (roadFill < 1)
                    regenerate = true;
                else
                    roadAnimationPlaying = false;
            }

            // Generate heights
            GenerateHeightsForMesh(pointsToAvoid, roadMeshCreator.roadWidth, roadSmoothDistance);
        }




        // Spawn house
        if (generateHouse)
        {
            var houseObj = house.Generate(xsize, ysize, pointsToAvoid, houseDistanceFromPath, houseDistanceFromEdge, houseAnimation, seed);
            if (houseObj != null)
            {
                toDeleteList.Add(houseObj);
                houseAnimation = false;
            }
        }


        // Generate river
        riverSpawner.gameObject.SetActive(generateRiver);
        if (generateRiver)
        {
            RiverSpawner.xsize = xsize;
            RiverSpawner.ysize = ysize;
            RiverSpawner.mesh = mesh;
            RiverSpawner.edgeVertices = edgeVertices;
            RiverSpawner.elevations = elevations;
            RiverSpawner.riverHeightOffset = riverHeightOffset;

            riverSpawner.Generate();
            pointsToAvoid.Clear();
            pointsToAvoid.AddRange(riverSpawner.pathCreator.path.GeneratePointsAlongPath(5));

            // Generate heights
            GenerateHeightsForMesh(pointsToAvoid, riverFlatDistance, riverSmoothDistance);
        }

        // Make terrain surface mesh
        MakeMesh();


        // Make base meshes
        if (generateBase)
        {
            terrainBase.elevations = elevations;
            terrainBase.xsize = xsize;
            terrainBase.ysize = ysize;
            terrainBase.topLayerSize = topLayerSize;
            terrainBase.bottomLayerSize = bottomLayerSize;
            toDeleteList.AddRange(terrainBase.MakeBase(edgeVertices, baseAnimation));

            baseAnimation = false;
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
                toDeleteList,
                rocksAnimation,
                seed)
            );
            rocksAnimation = false;
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
                toDeleteList,
                treesAnimation,
                seed)
            );
            treesAnimation = false;
        }
    }




    public void GenerateHeightsForMesh (List<Vector3> pointsToAvoid = null, float avoidWidth = 7, float avoidSmooth = 30)
    {
        bool elevationsExist = false;
        if (elevations.Count == mesh.vertices.Count)
            elevationsExist = true;

        edgeVertices.Clear();

        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            // Base height from perlin noise
            float elevation = 0;
            if (!elevationsExist)
                elevation = GetPerlinElevation(new Vector2((float)mesh.vertices[i].x, (float)mesh.vertices[i].y));
            else
                elevation = elevations[i];

            // Fix height along path
            var roadWidth = (avoidWidth + minPointRadius + 6);
            if (pointsToAvoid != null && pointsToAvoid.Count > 0)
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
                else if (distToRoad > roadWidth && distToRoad < avoidSmooth)
                {
                    elevation = map(distToRoad, roadWidth, avoidSmooth, closestPointOnRoad.y - 1, elevation);
                }
            }

            if (!elevationsExist)
                elevations.Add(elevation);
            else
                elevations[i] = elevation;
        }

        // Collect all vertices on the edge of terrain for generatig base
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            if (mesh.vertices[i].label == 1)
            {
                edgeVertices.Add(mesh.vertices[i]);
            }
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
            float sample = (Mathf.PerlinNoise(seeds[o] + point.x * size / xsize * frequency,
                                              seeds[o] + point.y * size / ysize * frequency) - 0.5f) * amplitude;
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

            chunkMesh.MarkModified();

            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation, transform);
            toDeleteList.Add(chunk.gameObject);
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            chunk.GetComponent<MeshCollider>().sharedMesh = null;
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
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;

public class DelaunayTerrain : MonoBehaviour {
    [Header("Terrain")]
    public int seed = 0;

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
    public float roadSmoothDistance = 50;
    [Range(0,1f)]
    public float smoothMinValue = 0.58f;

    [Header("Trees")]
    public float treeMinPointRadius = 18;
    public float treeDistanceFromEdges = 6;
    private TreesSpawner treesSpawner;

    [Header("Rocks")]
    public float rockMinPointRadius = 47;
    public float rockDistanceFromEdges = 6;
    private RocksSpawner rocksSpawner;

    [Header("House")]
    public float houseDistanceFromPath = 35;
    public int houseDistanceFromEdge = 35;
    private House house;


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
        house = GetComponentInChildren<House>();

        Generate();
    }

    void Update() {
        if (regenerate) {
            regenerate = false;

            // Clear playing animations
            StopAllCoroutines();
            DOTween.Clear();

            // Delete everything on delete list
            foreach (var gameObject in toDelete) {
                Destroy(gameObject);
            }
            toDelete.Clear();

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

    public virtual void Generate() {
        var rng = new RandomNumbers(seed);

        elevations = new List<float>();

        float[] seeds = new float[octaves];

        for (int i = 0; i < octaves; i++) {
            seeds[i] = rng.Range(0.0f, 100.0f);
        }
        
        Polygon polygon = new Polygon();


        // Randomise path
        var pointsToAvoid = new List<Vector3>();
        if (generateRoad)
        {
            roadMeshCreator.gameObject.SetActive(true);
            // Edge point
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(rng.Range(30, xsize - 30), 0, ysize - 0.015f), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(rng.Range(30, xsize - 30), 0, 0.015f), true);
            // Middle point and its handles
            var handle = new Vector3(rng.Range(10, xsize - 10), 0, rng.Range((ysize / 2) + 10, ysize));
            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, handle, true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(xsize / 2 + rng.Range(-15,15), 0, ysize / 2 + rng.Range(-15, 15)), true);
            // Update road mesh
            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();
            roadMeshCreator.UpdateMesh();

            // Generate points spaced along path
            pointsToAvoid = roadMeshCreator.pathCreator.path.GeneratePointsAlongPath(15);
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
        for (int i = 0; i < randomPoints; i++) {
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
            float elevation = 0.0f;
            float amplitude = Mathf.Pow(persistence, octaves);
            float frequency = 1.0f;
            float maxVal = 0.0f;

            for (int o = 0; o < octaves; o++)
            {
                float sample = (Mathf.PerlinNoise(seeds[o] + (float)mesh.vertices[i].x * sampleSize / (float)xsize * frequency,
                                                  seeds[o] + (float)mesh.vertices[i].y * sampleSize / (float)ysize * frequency) - 0.5f) * amplitude;
                elevation += sample;
                maxVal += amplitude;
                amplitude /= persistence;
                frequency *= frequencyBase;
            }

            elevation = elevation / maxVal;
            elevation *= elevationScale;


            // Fix height along path
            if (pointsToAvoid.Count > 0)
            {
                foreach (var point in pointsToAvoid)
                {
                    var Point2Da = new Vector2(point.x, point.z);
                    var Point2Db = new Vector2((float)mesh.vertices[i].x, (float)mesh.vertices[i].y);
                    var distSqr = (Point2Da - Point2Db).sqrMagnitude;
                    var roadWidth = roadMeshCreator.roadWidth + minPointRadius + 6;

                    if (distSqr <= roadWidth * roadWidth) elevation = -1;
                    else if (distSqr > roadWidth * roadWidth && distSqr < roadSmoothDistance * roadSmoothDistance)
                    {
                        elevation *= map(distSqr, roadWidth * roadWidth, roadSmoothDistance * roadSmoothDistance, smoothMinValue, 1);
                    }
                }
            }

            elevations.Add(elevation);
        }


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

        //foreach (var item in pointsToAvoid)
        //{
        //    Debug.DrawLine(item, item + Vector3.up * 20, Color.red, 15);
        //}
    }

    public void MakeMesh() {
        IEnumerator<Triangle> triangleEnumerator = mesh.Triangles.GetEnumerator();

        for (int chunkStart = 0; chunkStart < mesh.Triangles.Count; chunkStart += trianglesInChunk) {
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

    public Vector3 GetPoint3D(int index) {
        Vertex vertex = mesh.vertices[index];
        float elevation = elevations[index];
        return new Vector3((float)vertex.x, elevation, (float)vertex.y);
    }

    float map(float s, float a1, float a2, float b1, float b2)
    {
        return b1 + (s - a1) * (b2 - b1) / (a2 - a1);
    }

}
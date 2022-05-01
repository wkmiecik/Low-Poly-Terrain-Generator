using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing;
using DG.Tweening;

public class DelaunayTerrain : MonoBehaviour {
    [Header("Terrain")]
    public int seed = 0;

    public int xsize = 50;
    public int ysize = 50;

    public float minPointRadius = 4.0f;

    public int randomPoints = 100;

    public int trianglesInChunk = 20000;

    public float elevationScale = 100.0f;
    public float sampleSize = 1.0f;
    public int octaves = 8;
    public float frequencyBase = 2;
    public float persistence = 1.1f;

    public Transform chunkPrefab = null;

    private List<float> elevations;

    [HideInInspector] public static TriangleNet.Mesh mesh = null;

    public float vertexMergeSize = 0;
    public float vertexEdgeMergeDistance = 0;

    [SerializeField] bool regenerate = false;

    private static List<Vertex> edgeVertices;
    TerrainBase terrainBase;


    [Header("Road")]
    public RoadMeshCreator roadMeshCreator;
    public float roadSmoothDistance;
    [Range(0,1f)]
    public float smoothMinValue;


    [Header("Trees")]
    public float treeMinPointRadius = 17;
    public float distanceFromEdges = 10;
    private TreesSpawner treesSpawner;


    [Header("Generation steps")]
    public bool generateBase = true;
    public bool generateRoad = true;
    public bool generateTrees = true;

    private void Start()
    {
        terrainBase = GetComponentInChildren<TerrainBase>();
        treesSpawner = GetComponentInChildren<TreesSpawner>();
        Generate();
    }

    void Update() {
        if (regenerate) {
            regenerate = false;

            StopAllCoroutines();
            DOTween.Clear();

            GameObject[] chunks = GameObject.FindGameObjectsWithTag("chunk");
            foreach (GameObject chunk in chunks) {
                Destroy(chunk);
            }

            GameObject[] trees = GameObject.FindGameObjectsWithTag("tree");
            foreach (GameObject tree in trees)
            {
                Destroy(tree);
            }

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
        Random.InitState(seed);

        elevations = new List<float>();

        float[] seeds = new float[octaves];

        for (int i = 0; i < octaves; i++) {
            seeds[i] = Random.Range(0.0f, 100.0f);
        }
        
        Polygon polygon = new Polygon();


        // Randomise path
        if (generateRoad)
        {
            roadMeshCreator.gameObject.SetActive(true);
            // Edge point
            roadMeshCreator.pathCreator.bezierPath.MovePoint(0, new Vector3(Random.Range(30, xsize - 30), 0, ysize - 0.015f), true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(6, new Vector3(Random.Range(30, xsize - 30), 0, 0.015f), true);
            // Middle point and its handles
            var handle = new Vector3(Random.Range(10, xsize - 10), 0, Random.Range((ysize / 2) + 10, ysize));
            roadMeshCreator.pathCreator.bezierPath.MovePoint(2, handle, true);
            roadMeshCreator.pathCreator.bezierPath.MovePoint(3, new Vector3(xsize / 2 + Random.Range(-15,15), 0, ysize / 2 + Random.Range(-15, 15)), true);
            // Update road mesh
            roadMeshCreator.pathCreator.bezierPath.NotifyPathModified();
            roadMeshCreator.UpdateMesh();
        } 
        else
        {
            roadMeshCreator.gameObject.SetActive(false);
        }
        // Generate points spaced along path
        var pointsAlongPath = roadMeshCreator.pathCreator.path.GeneratePointsAlongPath(15);


        //Add uniformly-spaced points
        foreach (Vector2 sample in PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize, ysize)))
        {
            polygon.Add(new Vertex((double)sample.x, (double)sample.y));
        }

        // Add some randomly sampled points
        for (int i = 0; i < randomPoints; i++) {
            polygon.Add(new Vertex(Random.Range(0.0f, xsize), Random.Range(0.0f, ysize)));
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
            if (generateRoad)
            {
                foreach (var point in pointsAlongPath)
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


        // Collet all vertices on the edge of terrain for generatig base
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
            terrainBase.MakeBase(edgeVertices);
        }


        // Spawn trees
        if (generateTrees)
        {
            StartCoroutine(treesSpawner.Generate(xsize, ysize, treeMinPointRadius, distanceFromEdges, pointsAlongPath, roadMeshCreator.roadWidth + minPointRadius + 6));
        }
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
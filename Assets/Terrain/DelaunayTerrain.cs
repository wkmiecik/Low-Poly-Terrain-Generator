﻿using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
using TriangleNet.Topology;
using TriangleNet.Meshing.Algorithm;
using TriangleNet.Meshing;

public class DelaunayTerrain : MonoBehaviour {
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

    private void Start()
    {
        terrainBase = GetComponentInChildren<TerrainBase>();
    }

    void Update() {
        if (regenerate) {
            regenerate = false;
            GameObject[] chunks = GameObject.FindGameObjectsWithTag("chunk");
            foreach (GameObject chunk in chunks) {
                Destroy(chunk);
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
        
        PoissonDiscSampler sampler = new PoissonDiscSampler(xsize, ysize, minPointRadius);

        Polygon polygon = new Polygon();

        // Add uniformly-spaced points
        foreach (Vector2 sample in sampler.Samples()) {
            polygon.Add(new Vertex((double)sample.x, (double)sample.y));
        }

        // Add some randomly sampled points
        for (int i = 0; i < randomPoints; i++) {
            polygon.Add(new Vertex(Random.Range(0.0f, xsize), Random.Range(0.0f, ysize)));
        }
        // Add corner points doubled
        polygon.Add(new Vertex(0, 0, 1));
        polygon.Add(new Vertex(0, 300, 1));
        polygon.Add(new Vertex(300, 0, 1));
        polygon.Add(new Vertex(300, 300, 1));

        ConstraintOptions options = new ConstraintOptions() { ConformingDelaunay = true };
        mesh = (TriangleNet.Mesh)polygon.Triangulate(options);
        
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            if (Mathf.Abs((float)mesh.vertices[i].x - xsize) < vertexEdgeMergeDistance)
            {
                mesh.vertices[i].x = xsize;
            }
            if (mesh.vertices[i].x < vertexEdgeMergeDistance)
            {
                mesh.vertices[i].x = 0;
            }
            if (Mathf.Abs((float)mesh.vertices[i].y - ysize) < vertexEdgeMergeDistance)
            {
                mesh.vertices[i].y = xsize;
            }
            if (mesh.vertices[i].y < vertexEdgeMergeDistance)
            {
                mesh.vertices[i].y = 0;
            }

            for (int j = i + 1; j < mesh.vertices.Count; j++)
            {
                var v1 = new Vector2((float)mesh.vertices[i].x, (float)mesh.vertices[i].y);
                var v2 = new Vector2((float)mesh.vertices[j].x, (float)mesh.vertices[j].y);
                if (Vector2.Distance(v1, v2) < vertexMergeSize)
                {
                    mesh.vertices[j] = mesh.vertices[i];
                }
            }


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
            elevations.Add(elevation * elevationScale);
        }

        edgeVertices = new List<Vertex>();
        for (int i = 0; i < mesh.vertices.Count; i++)
        {
            if (mesh.vertices[i].label == 1)
            {
                edgeVertices.Add(mesh.vertices[i]);
            }
        }

        MakeMesh();
        terrainBase.elevations = elevations;
        terrainBase.MakeBase(edgeVertices, xsize, ysize);
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

            Transform chunk = Instantiate<Transform>(chunkPrefab, transform.position, transform.rotation);
            chunk.GetComponent<MeshFilter>().mesh = chunkMesh;
            chunk.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
            chunk.transform.parent = transform;
        }
    }

    public Vector3 GetPoint3D(int index) {
        Vertex vertex = mesh.vertices[index];
        float elevation = elevations[index];
        return new Vector3((float)vertex.x, elevation, (float)vertex.y);
    }
}
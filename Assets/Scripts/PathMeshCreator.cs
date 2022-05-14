using System.Collections.Generic;
using PathCreation.Utility;
using UnityEngine;
using PathCreation;

[ExecuteInEditMode, RequireComponent(typeof(MeshRenderer)), RequireComponent(typeof(MeshFilter))]
public class PathMeshCreator : MonoBehaviour
{
    [Header("Path")]
    public PathCreator pathCreator;

    [Header("Road settings")]
    public float roadWidth = .4f;
    [Range(0, 10f)]
    public float thickness = .15f;
    public bool flattenSurface;
    public float heightOffset = 1;

    [Header("Material settings")]
    public Material roadMaterial;
    public Material undersideMaterial;
    public float textureTiling = 1;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh mesh;

    public int viewedVertexIndex = 0;

    public bool manualUpdate = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (mesh == null)
        {
            mesh = new Mesh();
        }
        meshFilter.sharedMesh = mesh;
        meshRenderer = GetComponent<MeshRenderer>();

        pathCreator.InitializeEditorData(true);
    }

    private void Update()
    {
        if (manualUpdate)
        {
            manualUpdate = false;
            UpdateMesh();
        }

        if (Application.isPlaying)
        {
            meshRenderer.enabled = true;
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }

    public void UpdateMesh(float fill = 1)
    {
        int len = Mathf.CeilToInt(pathCreator.path.NumPoints * fill);

        if (len >= 1)
        {
            var points = new Vector3[len];
            var tangents = new Vector3[len];
            var normals = new Vector3[len];

            for (int i = 0; i < len; i++)
            {
                points[i] = pathCreator.path.GetPoint(i);
                tangents[i] = pathCreator.path.GetTangent(i);
                normals[i] = pathCreator.path.GetNormal(i);
            }
            CreateRoadMesh(points, tangents, normals);

            transform.position = new Vector3(0, heightOffset, 0);
        }
    }


    void CreateRoadMesh(Vector3[] points, Vector3[] tangents, Vector3[] normals)
    {
        Vector3[] verts = new Vector3[(points.Length * 8) + 8];

        int numTris = 2 * (points.Length - 1) + (pathCreator.path.isClosedLoop ? 2 : 0);
        int[] roadTriangles = new int[numTris * 3];
        int[] sideOfRoadTriangles = new int[numTris * 2 * 3];
        int[] capTriangles = new int[12];

        int vertIndex = 0;
        int triIndex = 0;

        int[] triangleMap = { 0, 8, 1, 1, 8, 9 };
        int[] sidesTriangleMap = { 4, 6, 14, 12, 4, 14, 5, 15, 7, 13, 15, 5 };

        for (int i = 0; i < points.Length; i++)
        {
            Vector3 localUp = (flattenSurface) ? Vector3.up : Vector3.Cross(tangents[i], normals[i]);
            Vector3 localRight = (flattenSurface) ? Vector3.Cross(localUp, tangents[i]) : normals[i];

            // Find position to left and right of current path vertex
            Vector3 vertSideA = points[i] - localRight * Mathf.Abs(roadWidth);
            Vector3 vertSideB = points[i] + localRight * Mathf.Abs(roadWidth);

            if (vertSideA[0] <= 0) vertSideA[0] = 0.015f;
            if (vertSideA[0] >= 400) vertSideA[0] = 400 - 0.015f;
            if (vertSideA[2] <= 0) vertSideA[2] = 0.015f;
            if (vertSideA[2] >= 400) vertSideA[2] = 400 - 0.015f;

            if (vertSideB[0] <= 0) vertSideB[0] = 0.015f;
            if (vertSideB[0] >= 400) vertSideB[0] = 400 - 0.015f;
            if (vertSideB[2] <= 0) vertSideB[2] = 0.015f;
            if (vertSideB[2] >= 400) vertSideB[2] = 400 - 0.015f;

            // Add top of road vertices
            verts[vertIndex + 0] = vertSideA;
            verts[vertIndex + 1] = vertSideB;
            // Add bottom of road vertices
            verts[vertIndex + 2] = vertSideA - localUp * thickness;
            verts[vertIndex + 3] = vertSideB - localUp * thickness;

            // Duplicate vertices to get flat shading for sides of road
            verts[vertIndex + 4] = verts[vertIndex + 0];
            verts[vertIndex + 5] = verts[vertIndex + 1];
            verts[vertIndex + 6] = verts[vertIndex + 2];
            verts[vertIndex + 7] = verts[vertIndex + 3];

            // Set triangle indices
            if (i < points.Length - 1 || pathCreator.path.isClosedLoop)
            {
                for (int j = 0; j < triangleMap.Length; j++)
                {
                    roadTriangles[triIndex + j] = (vertIndex + triangleMap[j]) % verts.Length;
                }
                for (int j = 0; j < sidesTriangleMap.Length; j++)
                {
                    sideOfRoadTriangles[triIndex * 2 + j] = (vertIndex + sidesTriangleMap[j]) % verts.Length;
                }
            }

            vertIndex += 8;
            triIndex += 6;
        }

        // Caps front
        verts[verts.Length - 1] = verts[0];
        verts[verts.Length - 2] = verts[1];
        verts[verts.Length - 3] = verts[2];
        verts[verts.Length - 4] = verts[3];
        capTriangles[0] = verts.Length - 2;
        capTriangles[1] = verts.Length - 3;
        capTriangles[2] = verts.Length - 1;
        capTriangles[3] = verts.Length - 4;
        capTriangles[4] = verts.Length - 3;
        capTriangles[5] = verts.Length - 2;

        // Caps back
        verts[verts.Length - 5] = verts[verts.Length - 9];
        verts[verts.Length - 6] = verts[verts.Length - 10];
        verts[verts.Length - 7] = verts[verts.Length - 11];
        verts[verts.Length - 8] = verts[verts.Length - 12];
        capTriangles[6] = verts.Length - 5;
        capTriangles[7] = verts.Length - 7;
        capTriangles[8] = verts.Length - 6;
        capTriangles[9] = verts.Length - 6;
        capTriangles[10] = verts.Length - 7;
        capTriangles[11] = verts.Length - 8;

        mesh.Clear();
        mesh.vertices = verts;
        mesh.subMeshCount = 3;
        mesh.SetTriangles(roadTriangles, 0);
        mesh.SetTriangles(sideOfRoadTriangles, 2);
        mesh.SetTriangles(capTriangles, 1);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}

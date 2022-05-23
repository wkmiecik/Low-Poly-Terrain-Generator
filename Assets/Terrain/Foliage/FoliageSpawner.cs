using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FoliageSpawner : MonoBehaviour
{
    [Header("Trees")]
    public List<GameObject> treePrefabs;
    public Gradient treeGradient;

    [Header("Grass")]
    public List<GameObject> grassPrefabs;
    public List<Mesh> grassMeshes;
    public Material grassMaterial;

    private List<CombineInstance> combineMeshes = new List<CombineInstance>();

    public IEnumerator GenerateTrees(
        float xsize,
        float ysize,
        float minPointRadius,
        float distanceFromEdges,
        List<Vector3> pointsToAvoid,
        float pointsAvoidDistance,
        List<GameObject> toDelete,
        bool animate = false,
        int seed = 100)
    {
        RandomNumbers rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add trees
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(200 * Time.deltaTime) == 0 && animate) 
                yield return null;

            // Doing it every loop so seed is not depending on amount of spawned objects
            var rot = Quaternion.Euler(rng.Range(-5, 5), rng.Range(0, 360), rng.Range(-5, 5));
            var scale = new Vector3(rng.Range(1.1f, 1.4f), rng.Range(1.1f, 1.4f), rng.Range(1.1f, 1.4f));
            var color = treeGradient.Evaluate(rng.Range(0f, 1f));
            var prefab = treePrefabs[rng.Range(0, treePrefabs.Count)];

            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges/2, 100, samples[i].y + distanceFromEdges/2);

            if (Physics.Raycast(rayStartPos, -Vector3.up, out hit))
            {
                bool spawn = true;

                // Check if not on avoid point
                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                // Check if slope is not too big
                if (hit.normal.y < .8f)
                {
                    spawn = false;
                }

                if (spawn)
                {
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up, rot, scale, color, animate));
                }
            }
        }
    }


    public IEnumerator GenerateGrass(
    float xsize,
    float ysize,
    float minPointRadius,
    float distanceFromEdges,
    List<Vector3> pointsToAvoid,
    float pointsAvoidDistance,
    List<GameObject> toDelete,
    bool animate = false,
    int seed = 100)
    {
        RandomNumbers rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samplesGrass = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add grass
        for (int i = 0; i < samplesGrass.Count; i++)
        {
            if (i % Mathf.CeilToInt(600 * Time.deltaTime) == 0 && animate)
                yield return null;

            // Doing it every loop so seed is not depending on amount of spawned objects
            var scale = new Vector3(rng.Range(70, 80), rng.Range(70, 80), rng.Range(70, 80));
            int rndModel = rng.Range(0, grassMeshes.Count);
            var prefab = grassPrefabs[rndModel];
            var mesh = grassMeshes[rndModel];

            var rayStartPos = new Vector3(samplesGrass[i].x + distanceFromEdges / 2, 100, samplesGrass[i].y + distanceFromEdges / 2);

            if (Physics.Raycast(rayStartPos, -Vector3.up, out hit))
            {
                bool spawn = true;

                // Check if not on avoid point
                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                // Check if slope is not too big
                if (hit.normal.y < .7f)
                {
                    spawn = false;
                }
                var rot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                if (spawn)
                {
                    if (animate)
                        toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, scale, null, animate));
                    else
                        AddCombineMesh(mesh, hit.point + Vector3.up * -1f, rot, scale);
                }
            }
        }
        if (!animate)
        {
            toDelete.Add(SpawnCombined());
            combineMeshes.Clear();
        }
    }


    private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Vector3 scale, Color? color, bool animate)
    {
        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

        if (color != null)
        {
            var meshRenderer = obj.GetComponent<MeshRenderer>();
            var mat = meshRenderer.materials[0];
            mat.color = (Color)color;
            meshRenderer.materials[0] = mat;
        }

        if (animate)
        {
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(scale, .3f);
        }
        else
        {
            obj.transform.localScale = scale;
        }

        return obj;
    }


    private void AddCombineMesh(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        CombineInstance instance = new CombineInstance();
        instance.mesh = mesh;
        instance.transform = Matrix4x4.TRS(pos, rot, scale);

        combineMeshes.Add(instance);
    }

    private GameObject SpawnCombined()
    {
        var obj = new GameObject("Grass");
        obj.transform.parent = transform;

        var meshFilter = obj.AddComponent<MeshFilter>();
        meshFilter.mesh = new Mesh();
        meshFilter.mesh.CombineMeshes(combineMeshes.ToArray());

        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = grassMaterial;

        return obj;
    }
}

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
    public Gradient grassGradient;


    RandomNumbers rng;

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
        rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add trees
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(200 * Time.deltaTime) == 0 && animate) 
                yield return null;

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
                    var rot = Quaternion.Euler(rng.Range(-5, 5), rng.Range(0, 360), rng.Range(-5, 5));
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up, rot, 1.1f, 1.4f, treeGradient, animate));
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
        rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samplesGrass = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges), seed: seed);

        //Add grass
        for (int i = 0; i < samplesGrass.Count; i++)
        {
            if (i % Mathf.CeilToInt(600 * Time.deltaTime) == 0 && animate)
                yield return null;

            var prefab = grassPrefabs[rng.Range(0, treePrefabs.Count)];
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

                if (spawn)
                {
                    var rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                    toDelete.Add(Spawn(prefab, hit.point + Vector3.up * -1f, rot, 70, 80, grassGradient, animate));
                }
            }
        }
    }


    private GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, float scaleMin, float scaleMax, Gradient gradient, bool animate)
    {
        var seedSave = rng.seed;

        
        var scale = new Vector3(rng.Range(scaleMin, scaleMax), rng.Range(scaleMin, scaleMax), rng.Range(scaleMin, scaleMax));

        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

        var meshRenderer = obj.GetComponent<MeshRenderer>();
        var mat = meshRenderer.materials[0];
        mat.color = gradient.Evaluate(rng.Range(0f, 1f));
        meshRenderer.materials[0] = mat;

        if (animate)
        {
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(scale, .3f);
        }
        else
        {
            obj.transform.localScale = scale;
        }

        rng.seed = seedSave;
        return obj;
    }
}

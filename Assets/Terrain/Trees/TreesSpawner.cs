using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TreesSpawner : MonoBehaviour
{
    public List<GameObject> treePrefabs;
    public Gradient gradient;

    RandomNumbers rng;

    public IEnumerator Generate(float xsize, float ysize, float minPointRadius, float distanceFromEdges, List<Vector3> pointsToAvoid, float pointsAvoidDistance, int seed = 100)
    {
        rng = new RandomNumbers(seed);

        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges));

        //Add uniformly-spaced points
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(400 * Time.deltaTime) == 0) yield return null;

            var prefab = treePrefabs[rng.Range(0, treePrefabs.Count)];
            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges/2, 50, samples[i].y + distanceFromEdges/2);

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
                    spawn = false;

                if (spawn) 
                    SpawnTree(prefab, hit.point + Vector3.up); 
            }
        }
    }


    private void SpawnTree(GameObject prefab, Vector3 pos)
    {
        var rot = Quaternion.Euler(rng.Range(-5, 5), rng.Range(0, 360), rng.Range(-5, 5));
        var scale = new Vector3(1 + rng.Range(0.1f, 0.4f), 1 + rng.Range(0.1f, 0.4f), 1 + rng.Range(0.1f, 0.4f));

        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

        var meshRenderer = obj.GetComponent<MeshRenderer>();
        var mat = meshRenderer.materials[0];
        mat.color = gradient.Evaluate(rng.Range(0f, 1f));
        meshRenderer.materials[0] = mat;


        obj.transform.localScale = Vector3.zero;
        obj.transform.DOScale(scale, .3f);
    }
}

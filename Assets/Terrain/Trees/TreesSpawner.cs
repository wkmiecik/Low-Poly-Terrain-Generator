using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreesSpawner : MonoBehaviour
{
    public List<GameObject> treePrefabs;

    public IEnumerator Generate(float xsize, float ysize, float minPointRadius, float distanceFromEdges, List<Vector3> pointsToAvoid, float pointsAvoidDistance)
    {
        RaycastHit hit;

        var samples = PoissonDiscSampler.GeneratePoints(minPointRadius, new Vector2(xsize - distanceFromEdges, ysize - distanceFromEdges));

        //Add uniformly-spaced points
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % 5 == 0) yield return null;

            var prefab = treePrefabs[Random.Range(0, treePrefabs.Count)];
            var pos = new Vector3(samples[i].x + distanceFromEdges/2, 50, samples[i].y + distanceFromEdges/2);
            var rot = Quaternion.Euler(-90, Random.Range(0, 360), 0);
            var scale = new Vector3(1 + Random.Range(0.1f, 0.4f), 1 + Random.Range(0.1f, 0.4f), 1 + Random.Range(0.1f, 0.4f));

            if (Physics.Raycast(pos, -Vector3.up, out hit))
            {
                bool spawn = true;

                foreach (var point in pointsToAvoid)
                {
                    if ((hit.point - point).sqrMagnitude <= pointsAvoidDistance * pointsAvoidDistance)
                    {
                        spawn = false;
                    }
                }

                if (spawn)
                {
                    var obj = Instantiate(prefab, hit.point, rot);
                    obj.transform.parent = transform;
                    obj.transform.localScale = scale;
                }
            }
        }
    }
}

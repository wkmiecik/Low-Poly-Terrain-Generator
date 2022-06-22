using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RocksSpawner : MonoBehaviour
{
    public List<GameObject> rockPrefabs;
    public Gradient gradient;

    private const string baseTag = "baseBottom";

    private RandomNumbers rng;

    public IEnumerator Generate(
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

        // Add uniformly-spaced rocks
        for (int i = 0; i < samples.Count; i++)
        {
            if (i % Mathf.CeilToInt(2 * Time.deltaTime) == 0 && animate) 
                yield return new WaitForSeconds(.1f);

            var prefab = rockPrefabs[rng.Range(0, rockPrefabs.Count)];
            var rayStartPos = new Vector3(samples[i].x + distanceFromEdges / 2, 100, samples[i].y + distanceFromEdges / 2);

            if (Physics.Raycast(rayStartPos, Vector3.down, out hit))
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
                    toDelete.Add(Spawn(prefab, hit.point, animate));
                }
            }
        }

        // Add base rocks
        for (int i = 0; i < 10; i++)
        {
            RaycastOnBase(new Vector3(-50, rng.Range(-120, 70), rng.Range(0, xsize)), Vector3.right);
            RaycastOnBase(new Vector3(xsize + 50, rng.Range(-120, 70), rng.Range(0, xsize)), Vector3.left);
            RaycastOnBase(new Vector3(rng.Range(0, ysize), rng.Range(-120, 70), -50), Vector3.forward);
            RaycastOnBase(new Vector3(rng.Range(0, ysize), rng.Range(-120, 70), ysize + 50), Vector3.back);
        }

        void RaycastOnBase(Vector3 rayStartPos, Vector3 dir)
        {
            var prefab = rockPrefabs[rng.Range(0, rockPrefabs.Count)];

            if (Physics.Raycast(rayStartPos, dir, out hit))
            {
                if (hit.collider.CompareTag(baseTag))
                {
                    toDelete.Add(Spawn(prefab, hit.point, animate, 3.5f, 4.5f));
                }
            }
        }
    }


    private GameObject Spawn(GameObject prefab, Vector3 pos, bool animate, float minScale = 2f, float maxScale = 3f)
    {
        var seedSave = rng.seed;

        var rot = Quaternion.Euler(rng.Range(0, 360), rng.Range(0, 360), rng.Range(0, 360));
        var scale = new Vector3(1 + rng.Range(minScale, maxScale), 1 + rng.Range(minScale, maxScale), 1 + rng.Range(minScale, maxScale));

        var obj = Instantiate(prefab, transform, true);
        obj.transform.SetPositionAndRotation(pos, rot);
        obj.transform.parent = transform;

        var meshRenderer = obj.GetComponent<MeshRenderer>();
        var mat = meshRenderer.materials[0];
        mat.color = gradient.Evaluate(rng.Range(0f, 1f));
        meshRenderer.materials[0] = mat;

        if (animate)
        {
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(scale, 1.5f);
        } else
        {
            obj.transform.localScale = scale;
        }

        rng.seed = seedSave;
        return obj;
    }
}

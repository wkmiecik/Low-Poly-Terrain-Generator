using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class LampsSpawner : MonoBehaviour
{
    public List<GameObject> lampPrefabs;

    [HideInInspector] public bool lampsGeneratedLeftSide;

    RandomNumbers rng;

    public void Generate(List<Vector3> pointsAlongRoad, int generateEveryNthPoint, float lampsDistanceFromRoad, bool animate, List<GameObject> toDelete, int seed)
    {
        rng = new RandomNumbers(seed);

        lampsGeneratedLeftSide = rng.Bool();

        var prefab = lampPrefabs[rng.Range(0, lampPrefabs.Count)];

        for (int i = 3; i < pointsAlongRoad.Count - 3; i += generateEveryNthPoint)
        {
            var heigth = pointsAlongRoad[i].y;

            int perpendicularCheckDst = 3;
            var pointBack = new Vector2(pointsAlongRoad[i - perpendicularCheckDst].x, pointsAlongRoad[i - perpendicularCheckDst].z);
            var point = new Vector2(pointsAlongRoad[i].x, pointsAlongRoad[i].z);

            var perpendicularBack = Vector2.Perpendicular(point - pointBack).normalized;

            var rotation1 = Mathf.Atan2(perpendicularBack.x, perpendicularBack.y) * Mathf.Rad2Deg;

            if (lampsGeneratedLeftSide)
            {
                point += (perpendicularBack * lampsDistanceFromRoad);
            }
            else
            {
                point -= (perpendicularBack * lampsDistanceFromRoad);
                rotation1 += 180;
            }


            var spawnPoint = new Vector3(point.x, heigth - 1, point.y);

            toDelete.Add(Spawn(prefab, spawnPoint, rotation1, animate));
        }
    }


    private GameObject Spawn(GameObject prefab, Vector3 pos, float rotEulerY, bool animate)
    {
        var seedSave = rng.seed;

        var rot = Quaternion.Euler(new Vector3(0, rotEulerY, 0));
        var scale = new Vector3(1 + rng.Range(0.1f, 0.4f), 1 + rng.Range(0.1f, 0.4f), 1 + rng.Range(0.1f, 0.4f));

        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

        if (animate)
        {
            obj.transform.localScale = Vector3.zero;
            obj.transform.DOScale(scale, .8f);
        }
        else
        {
            obj.transform.localScale = scale;
        }

        rng.seed = seedSave;
        return obj;
    }
}

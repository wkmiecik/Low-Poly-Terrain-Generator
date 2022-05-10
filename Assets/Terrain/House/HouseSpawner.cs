using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class HouseSpawner : MonoBehaviour
{
    public GameObject prefab;
    public Color wallColor;
    public Color roofColor;

    private RandomNumbers rng;

    public GameObject Generate(
        float xsize, 
        float ysize,
        List<Vector3> pointsToAvoid, 
        float houseDistanceFromPath,
        int houseDistanceFromEdge,
        bool animate = false,
        int seed = 100)
    {
        rng = new RandomNumbers(seed);

        int maxAttempts = 100;

        // Spawn in random position if there is no road
        if (pointsToAvoid.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < maxAttempts; i++)
        {
            if (pointsToAvoid.Count < 17) return null;
            var pointIndex = rng.Range(8, pointsToAvoid.Count - 8);

            var heigth = pointsToAvoid[pointIndex].y;

            var pointBack = new Vector2(pointsToAvoid[pointIndex - 6].x, pointsToAvoid[pointIndex - 6].z);
            var point = new Vector2(pointsToAvoid[pointIndex].x, pointsToAvoid[pointIndex].z);
            var pointFront = new Vector2(pointsToAvoid[pointIndex + 6].x, pointsToAvoid[pointIndex + 6].z);

            var perpendicularBack = Vector2.Perpendicular(point - pointBack).normalized;
            var perpendicularFront = Vector2.Perpendicular(pointFront - point).normalized;

            var rotation1 = Mathf.Atan2(perpendicularBack.x, perpendicularBack.y) * Mathf.Rad2Deg;
            var rotation2 = Mathf.Atan2(perpendicularFront.x, perpendicularFront.y) * Mathf.Rad2Deg;

            float rotationDiff;
            if (rng.Bool())
            {
                point += (perpendicularBack * houseDistanceFromPath);
                rotationDiff = rotation1 - rotation2;
            }
            else
            {
                point -= (perpendicularBack * houseDistanceFromPath);
                rotationDiff = rotation2 - rotation1;
                rotation1 += 180;
            }

            var spawnPoint = new Vector3(point.x, heigth, point.y);

            bool distanceFromTopBottom = spawnPoint.z > houseDistanceFromEdge && spawnPoint.z < ysize - houseDistanceFromEdge;
            bool distanceFromLeftRight = spawnPoint.x > houseDistanceFromEdge && spawnPoint.x < xsize - houseDistanceFromEdge;

            if (rotationDiff < 3 && distanceFromTopBottom && distanceFromLeftRight)
            {
                var obj = SpawnHouse(spawnPoint, rotation1, animate);
                pointsToAvoid.Add(spawnPoint);
                pointsToAvoid.Add(spawnPoint + new Vector3(perpendicularBack.x, 0, perpendicularBack.y) * 23);
                pointsToAvoid.Add(spawnPoint + Vector3.left * 23);
                pointsToAvoid.Add(spawnPoint + Vector3.right * 23);
                pointsToAvoid.Add(spawnPoint + Vector3.forward * 23);
                pointsToAvoid.Add(spawnPoint + Vector3.back * 23);
                return obj;
            }
        }
        return null;
    }


    private GameObject SpawnHouse(Vector3 pos, float rotEulerY, bool animate = false)
    {
        var rot = Quaternion.Euler(new Vector3(0, rotEulerY, 0));
        var scale = Vector3.one;

        var obj = Instantiate(prefab, pos, rot);
        obj.transform.parent = transform;

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
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : MonoBehaviour
{
    public List<GameObject> smokePrefabs;
    public Transform spawnPosition;
    public float spawnDelay = 1f;
    private float timer;

    RandomNumbers rng = new RandomNumbers(123);

    private void Start()
    {
        timer = spawnDelay;
    }

    void Update()
    {
        if (timer <= 0)
        {
            timer = spawnDelay;
            var obj = Instantiate(smokePrefabs[rng.Range(0, smokePrefabs.Count - 1)], spawnPosition.transform.position, Quaternion.identity, transform);
        }

        timer -= Time.deltaTime;
    }
}

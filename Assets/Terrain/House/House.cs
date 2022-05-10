using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class House : MonoBehaviour
{
    public List<GameObject> smokePrefabs;
    public Transform spawnPosition;
    public float spawnDelay = 1f;
    private float timer;

    RandomNumbers rng;

    private void Start()
    {
        rng = new RandomNumbers(GetInstanceID());
        timer = spawnDelay;
    }

    void Update()
    {
        if (timer <= 0)
        {
            timer = spawnDelay + rng.Range(-0.1f, 0.5f);
            var obj = Instantiate(smokePrefabs[rng.Range(0, smokePrefabs.Count - 1)], spawnPosition.transform.position, Quaternion.identity, transform);
        }

        timer -= Time.deltaTime;
    }
}

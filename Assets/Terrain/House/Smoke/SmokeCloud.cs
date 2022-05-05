using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SmokeCloud : MonoBehaviour
{
    public float speed = 10;
    private Vector3 moveDirection;

    private RandomNumbers rng;

    private float lifeTimer = 20;

    void Start()
    {
        rng = new RandomNumbers((int)(Time.time * 6));

        moveDirection = new Vector3(rng.Range(-0.2f, 0.2f), 1, rng.Range(-0.2f, 0.2f));

        var rot = Quaternion.Euler(rng.Range(0, 360), rng.Range(0, 360), rng.Range(0, 360));
        var scale = new Vector3(rng.Range(4.5f, 6), rng.Range(4.5f, 6), rng.Range(4.5f, 6));

        transform.rotation = rot;
        transform.localScale = Vector3.zero;
        transform.DOScale(scale, 6f);
    }

    void Update()
    {
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);

        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0) Destroy(gameObject);
    }
}

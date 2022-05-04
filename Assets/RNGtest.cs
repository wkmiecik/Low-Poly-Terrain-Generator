using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RNGtest : MonoBehaviour
{
    private static RandomNumbers rng = new RandomNumbers(100);

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.H))
        {
            Debug.Log(rng.Range(0,100));
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            rng = new RandomNumbers(1);
        }
    }
}

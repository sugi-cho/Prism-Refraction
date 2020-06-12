using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotater : MonoBehaviour
{
    public Vector3 pole;
    public float velocity;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(pole, velocity * Time.deltaTime);
    }
}

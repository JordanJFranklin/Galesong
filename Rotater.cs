using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Rotater : MonoBehaviour
{
    public bool isGlobal;
    public Vector3 RotationPerSecond;

    void Update()
    {
        if(!isGlobal)
        {
            transform.Rotate(RotationPerSecond * Time.deltaTime);
        }
        else
        {
            transform.eulerAngles += (RotationPerSecond * Time.deltaTime);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplePoint : MonoBehaviour
{
    public Transform HangPosition;

    private void Start()
    {
        if(HangPosition == null)
        {
            HangPosition = transform;
        }
    }
}

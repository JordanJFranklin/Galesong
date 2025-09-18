using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockOnTargetHelper : MonoBehaviour
{
    public GameObject lockOnEmpty;

    private void Start()
    {
        if(lockOnEmpty == null)
        {
            lockOnEmpty = gameObject;
        }
    }
}

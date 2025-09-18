using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrappleSwing : MonoBehaviour
{
    public float HookRange = 3;
    public GameObject Dir;
    public bool inUse;

    private GameObject Player;
    private Quaternion OriginalRot;

    void Start()
    {
        OriginalRot = transform.rotation;

        if(Dir != null)
        {
            Dir.transform.parent = null;
        }
    }

    void Update()
    {
        if(inUse)
        {
            FollowPlayerRotation();
        }
        else
        {
            OriginalRot = transform.rotation;
        }
    }

    private void FollowPlayerRotation()
    {
        //transform.rotation = Quaternion.LookRotation(Player.transform.forward, Player.transform.up);
    }

    public void AttachPlayer(GameObject player)
    {
        inUse = true;
        Player = player;
    }

    public void DetachPlayer()
    {
        inUse = false;
        Player = null;
    }
}

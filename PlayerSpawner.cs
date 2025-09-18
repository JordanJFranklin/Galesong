using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class PlayerSpawner : MonoBehaviour
{
    public bool isActiveSpawner;
    public GameObject currentPlayer;
    public GameObject Player;

    void Start()
    {
        if (Player != null && currentPlayer == null && isActiveSpawner)
        {
            SpawnPlayer();
        }
    }

    public void SpawnPlayer()
    {
        currentPlayer = Instantiate(Player, transform.position, transform.rotation);
        print("Player Has Respawned");
    }
}

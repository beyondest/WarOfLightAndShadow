using System;
using UnityEngine;
using UnityEngine.AI;

public class TestPrint : MonoBehaviour
{
    private void Start()
    {
        RegisterNavMeshObstacle();
    }

    void RegisterNavMeshObstacle()
    {
        var obstacles = FindObjectsByType<NavMeshObstacle>(FindObjectsSortMode.None);
        foreach (var obstacle in obstacles)
        {
            Debug.Log("SDSD");

            if (!obstacle.enabled)
            {
                obstacle.enabled = true; 
            }
        }
    }

}
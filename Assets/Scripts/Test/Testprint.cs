using System;
using UnityEngine;

namespace SparFlame.Test
{
    public class Testprint : MonoBehaviour
    {
        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.Return))
                Debug.Log("HAHAHA   ");
        }
    }
}
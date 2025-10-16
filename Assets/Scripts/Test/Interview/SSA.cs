using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Test.Interview
{
    public class SSA : MonoBehaviour
    {

        [DllImport("MyNativePlugin")]
        private static extern void GetPlayerData(IntPtr data);
        
        private void Update()
        {
            GetComponent<Collider>();
            gameObject.SetActive(false);
            transform.position = new float3();
        }
    }
}
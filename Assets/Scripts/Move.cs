using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class Move : MonoBehaviour
    {
        private void Update()
        {
            var newPos = transform.position;
            newPos.y -= 0.5f;
            transform.position = newPos;
        }
    }
}
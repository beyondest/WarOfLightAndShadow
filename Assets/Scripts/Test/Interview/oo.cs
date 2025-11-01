using System;
using System.Threading;
using Unity.Behavior;
using Unity.Jobs;
using UnityEngine;

namespace SparFlame.Test.Interview
{
    public class oo : MonoBehaviour
    {
        private void Update()
        {
            var a = GetComponent<BehaviorGraphAgent >();
            a.Update();
        }
    }
}
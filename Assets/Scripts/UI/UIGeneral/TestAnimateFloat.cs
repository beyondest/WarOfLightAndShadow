using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.UI.General
{
    public class TestAnimateFloat : MonoBehaviour
    {
        public AnimationCurve curve = AnimationCurve.EaseInOut(0,0,1,1);
        public float spaceDuration = 1.0f;

        public float returnDuration = 1.0f;
        public float to;
        public float targetValue;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                UIMathMethods.AnimateFloatOverCurve(() => targetValue, v => targetValue = v,spaceDuration,curve );
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                UIMathMethods.AnimateFloatOverCurve(() => targetValue, v => targetValue = v,returnDuration,curve,to );
            }
            var pos = transform.position;
            pos.y = targetValue;
            transform.position = pos;
        }
    }
}
using UnityEngine;
using System.Collections;
namespace SparFlame.UI.General
{


    public class UIBounce : MonoBehaviour
    {
        public float upAndDownDuration = 0.1f;
        public float scaleFactorUp = 1.1f;
        public float scaleFactorDown = 0.9f;
        public void PlayBounce()
        {
            StopAllCoroutines();
            StartCoroutine(BounceCoroutine());
        }

        IEnumerator BounceCoroutine()
        {
            Vector3 original = transform.localScale;
            Vector3 up = original * scaleFactorUp;
            Vector3 down = original * scaleFactorDown;

            float t = 0f;

            // 放大
            while (t < upAndDownDuration/2)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(original, up, t / upAndDownDuration);
                yield return null;
            }

            // 缩小
            t = 0f;
            while (t < upAndDownDuration/2)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(up, down, t / upAndDownDuration);
                yield return null;
            }

            // 回归
            transform.localScale = original;
        }
    }

}
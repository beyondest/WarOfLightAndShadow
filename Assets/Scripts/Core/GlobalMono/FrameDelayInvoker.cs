using UnityEngine;
using System;
using System.Collections;
namespace SparFlame.Core.GlobalMono
{

    public class FrameDelayInvoker : MonoBehaviour
    {
        public static FrameDelayInvoker Instance;
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        /// <summary>
        /// 在指定帧数后调用某个方法
        /// </summary>
        /// <param name="frames">要延迟的帧数</param>
        /// <param name="action">要执行的方法</param>
        public void InvokeAfterFrames(int frames, Action action)
        {
            StartCoroutine(InvokeAfterFramesCoroutine(frames, action));
        }

        private IEnumerator InvokeAfterFramesCoroutine(int frames, Action action)
        {
            for (int i = 0; i < frames; i++)
                yield return null; // 每帧等待一次

            action?.Invoke();
        }
    }

}
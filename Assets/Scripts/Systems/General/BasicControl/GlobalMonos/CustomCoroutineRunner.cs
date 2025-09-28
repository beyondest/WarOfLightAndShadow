using System.Collections;
using System.Collections.Generic;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl.GlobalMonos
{
    public class CustomCoroutineRunner : MonoBehaviour
    {
        public float checkInterval = 0.1f;
        public static CustomCoroutineRunner Instance;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public IEnumerator WhenAll(List<ResourceOperation> coroutines,
            ResourceLoadingUtils.LoadingProgress loadingProgress = null)
        {
            int finished = 0;
            int total = coroutines.Count;
            // 启动所有子协程
            foreach (var routine in coroutines)
            {
                StartCoroutine(Run(routine));
            }

            // 等待全部完成
            while (finished < total)
            {
                var avgLoading = 0f;
                foreach (var routine in coroutines)
                {
                    avgLoading += routine.Progress;
                }
                avgLoading /= total;
                loadingProgress?.Report(avgLoading);
                yield return new WaitForSecondsRealtime(checkInterval);
            }

            IEnumerator Run(IEnumerator routine)
            {
                yield return routine;
                finished++;
            }
        }
        
        public IEnumerator WhenAllInSequence(
            List<ResourceOperation> coroutines,
            ResourceLoadingUtils.LoadingProgress loadingProgress = null)
        {
            int finished = 0;
            int total = coroutines.Count;
            for (int i = 0; i < total; i++)
            {
                var routine = coroutines[i];

                // 执行当前操作
                yield return routine;

                // 完成计数
                finished++;

                // 更新进度
                float progress = (float)finished / total;
                loadingProgress?.Report(progress);
            }
        }

    }
}
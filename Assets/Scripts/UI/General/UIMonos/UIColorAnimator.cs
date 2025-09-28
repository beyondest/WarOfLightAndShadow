using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SparFlame.UI.General
{
    public class UIColorAnimator : MonoBehaviour
    {
        [Header("颜色设置")] public Color fromColor = Color.white;
        public Color toColor = Color.red;

        [Header("渐变控制")] [Tooltip("一次渐变的时长(秒)")]
        public float duration = 1f;

        [Tooltip("是否包含自身 Image 组件")] public bool includeSelfImage = true;

        [Tooltip("是否包含子物体的 Image 组件")] public bool includeChildrenImage;

        [Tooltip("是否包含自身 TMP_Text 组件")] public bool includeSelfText = true;

        [Tooltip("是否包含子物体的 TMP_Text 组件")] public bool includeChildrenText;

        [Tooltip("是否循环渐变(来回变色)")] public bool loop;

        private readonly List<Image> _images = new();
        private readonly List<TMP_Text> _texts = new();
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            CollectTargets();
        }

        /// <summary>
        /// 开始渐变
        /// </summary>
        public void StartFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            // 每次开始前重新收集，防止动态添加对象漏掉
            CollectTargets();
            _fadeCoroutine = StartCoroutine(FadeRoutine());
        }

        /// <summary>
        /// 暂停渐变
        /// </summary>
        public void PauseFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        /// <summary>
        /// 收集目标组件
        /// </summary>
        private void CollectTargets()
        {
            _images.Clear();
            _texts.Clear();

            if (includeSelfImage)
            {
                var img = GetComponent<Image>();
                if (img) _images.Add(img);
            }

            if (includeChildrenImage)
            {
                var childImages = GetComponentsInChildren<Image>(true);
                foreach (var img in childImages)
                {
                    if (!includeSelfImage && img.gameObject == gameObject) continue;
                    _images.Add(img);
                }
            }

            if (includeSelfText)
            {
                var txt = GetComponent<TMP_Text>();
                if (txt) _texts.Add(txt);
            }

            if (includeChildrenText)
            {
                var childTexts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var txt in childTexts)
                {
                    if (!includeSelfText && txt.gameObject == gameObject) continue;
                    _texts.Add(txt);
                }
            }
        }

        private IEnumerator FadeRoutine()
        {
            while (true)
            {
                yield return FadeOnce(fromColor, toColor);

                if (!loop) break;

                yield return FadeOnce(toColor, fromColor);
            }
        }

        private IEnumerator FadeOnce(Color start, Color end)
        {
            var time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                var t = Mathf.Clamp01(time / duration);
                var lerp = Color.Lerp(start, end, t);

                foreach (var img in _images)
                {
                    if (img) img.color = lerp;
                }

                foreach (var txt in _texts)
                {
                    if (txt) txt.color = lerp;
                }

                yield return null;
            }
        }

        private void OnDisable()
        {
            PauseFade();
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace SparFlame.UI.General
{
    public class UIFadeInOut : MonoBehaviour
    {
        public float fadeDuration = 0.3f;
        public bool useCanvasGroup;
        public bool fadeAllChildren;
        public bool fadeSelf = true;

        private CanvasGroup _canvasGroup;
        private Coroutine _currentFade;
        private readonly List<Image> _images = new();
        private readonly List<float> _startAlpha = new();
        void Awake()
        {
            if (useCanvasGroup)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                if (fadeSelf)
                {
                    _images.Add(GetComponent<Image>());
                }

                if (fadeAllChildren)
                {
                    _images.AddRange(GetComponentsInChildren<Image>());
                }

                if(_images.Count == 0)
                    Debug.LogError("FadeUI: No Image component found.");
                foreach (var image in _images)
                {
                    _startAlpha.Add(image.color.a);
                }
            }
        }

        public void FadeIn()
        {
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(Fade(1f));
        }

        public void FadeOut()
        {
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(Fade(0f));
        }

        private IEnumerator Fade(float targetAlpha)
        {
            var time = 0f;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                for(var i = 0; i < _images.Count; i++)
                {
                    var startAlpha = useCanvasGroup ? _canvasGroup.alpha : _startAlpha[i];
                    var alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);

                    if (useCanvasGroup)
                    {
                        _canvasGroup.alpha = alpha;
                    }
                    else
                    {
                        var c = _images[i].color;
                        c.a = alpha;
                        _images[i].color = c;
                    }
                }

                yield return null;
            }

            // Final correction
            if (useCanvasGroup)
                _canvasGroup.alpha = targetAlpha;
            else
            {
                foreach (var image in _images)
                {
                    var c = image.color;
                    c.a = targetAlpha;
                    image.color = c;
                }
            }

            _currentFade = null;
        }
    }
}
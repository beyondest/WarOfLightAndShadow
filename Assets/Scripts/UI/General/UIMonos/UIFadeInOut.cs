using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace SparFlame.UI.General
{
    public class UIFadeInOut : MonoBehaviour
    {
        public float fadeDuration = 0.3f;
        public bool useCanvasGroup;
        public bool fadeAllChildren;
        public bool fadeSelf = true;
        public bool ifStartShow;
        public float fadeInTargetAlpha = 1f;
        public float fadeOutTargetAlpha;
        private CanvasGroup _canvasGroup;
        private Coroutine _currentFade;

        private readonly List<Image> _images = new();
        private readonly List<float> _imageStartAlpha = new();

        private readonly List<TextMeshProUGUI> _texts = new();
        private readonly List<float> _textStartAlpha = new();

        private void Awake()
        {
            if (useCanvasGroup)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (!_canvasGroup)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            else
            {
                if (fadeSelf)
                {
                    var img = GetComponent<Image>();
                    if (img)
                        _images.Add(img);

                    var tmp = GetComponent<TextMeshProUGUI>();
                    if (tmp)
                        _texts.Add(tmp);
                }

                if (fadeAllChildren)
                {
                    _images.AddRange(GetComponentsInChildren<Image>(true));
                    _texts.AddRange(GetComponentsInChildren<TextMeshProUGUI>(true));
                }

                if (_images.Count == 0 && _texts.Count == 0)
                    Debug.LogWarning("FadeUI: No Image or TextMeshProUGUI component found.");

                foreach (var image in _images)
                    _imageStartAlpha.Add(image.color.a);

                foreach (var text in _texts)
                    _textStartAlpha.Add(text.color.a);
            }

            if (!ifStartShow)
            {
                foreach (var image in _images)
                {
                    var color = image.color;
                    color.a = 0;
                    image.color = color;
                }

                foreach (var text in _texts)
                {
                    var color = text.color;
                    color.a = 0;
                    text.color = color;
                }
            }
        }

        public void FadeIn()
        {
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(Fade(fadeInTargetAlpha));
        }

        public void FadeOut()
        {
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(Fade(fadeOutTargetAlpha));
        }

        public void FadeInThenFadeOut(float stayDelay )
        {
            if (_currentFade != null) StopCoroutine(_currentFade);
            _currentFade = StartCoroutine(FadeInThenOutCoroutine( stayDelay));
        }

        private IEnumerator FadeInThenOutCoroutine( float fadeOutDelay)
        {
            // 先淡入
            yield return Fade(1f);

            // 可选：淡入后停留一小段时间
            if (fadeOutDelay > 0f)
                yield return new WaitForSecondsRealtime(fadeOutDelay);

            // 再淡出
            yield return Fade(0f);

            _currentFade = null;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            var time = 0f;

            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(time / fadeDuration);

                if (useCanvasGroup)
                {
                    _canvasGroup.alpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, t);
                }
                else
                {
                    for (int i = 0; i < _images.Count; i++)
                    {
                        var color = _images[i].color;
                        color.a = Mathf.Lerp(_imageStartAlpha[i], targetAlpha, t);
                        _images[i].color = color;
                    }

                    for (int i = 0; i < _texts.Count; i++)
                    {
                        var color = _texts[i].color;
                        color.a = Mathf.Lerp(_textStartAlpha[i], targetAlpha, t);
                        _texts[i].color = color;
                    }
                }

                yield return null;
            }

            // Final correction
            if (useCanvasGroup)
            {
                _canvasGroup.alpha = targetAlpha;
            }
            else
            {
                foreach (var t in _images)
                {
                    var color = t.color;
                    color.a = targetAlpha;
                    t.color = color;
                }

                foreach (var t in _texts)
                {
                    var color = t.color;
                    color.a = targetAlpha;
                    t.color = color;
                }
            }

            _currentFade = null;
        }
    }
}

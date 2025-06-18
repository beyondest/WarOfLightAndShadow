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

        private CanvasGroup _canvasGroup;
        private Coroutine _currentFade;

        private readonly List<Image> _images = new();
        private readonly List<float> _imageStartAlpha = new();

        private readonly List<TextMeshProUGUI> _texts = new();
        private readonly List<float> _textStartAlpha = new();

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
                    var img = GetComponent<Image>();
                    if (img != null)
                        _images.Add(img);

                    var tmp = GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
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
                for (int i = 0; i < _images.Count; i++)
                {
                    var color = _images[i].color;
                    color.a = targetAlpha;
                    _images[i].color = color;
                }

                for (int i = 0; i < _texts.Count; i++)
                {
                    var color = _texts[i].color;
                    color.a = targetAlpha;
                    _texts[i].color = color;
                }
            }

            _currentFade = null;
        }
    }
}

using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    
    public class UIBlinker : MonoBehaviour
    {
        [SerializeField] private Color blinkColor = Color.red;
        [SerializeField] private float duration = 0.5f;

        private Image _image;
        private Color _originalColor;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _image = GetComponent<Image>();
            if (_image == null)
            {
                Debug.LogError("ColorBlinker requires an Image component.");
                enabled = false;
                return;
            }

            _originalColor = _image.color;
        }

        public void StartBlink()
        {
            StopBlink();
            _cts = new CancellationTokenSource();
            _ = BlinkLoopAsync(_cts.Token);
        }

        public void StopBlink()
        {
            _cts?.Cancel();
            _image.color = _originalColor;
        }

        private async Task BlinkLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await AnimateColorAsync(_image, _originalColor, blinkColor, duration / 2, token);
                    await AnimateColorAsync(_image, blinkColor, _originalColor, duration / 2, token);
                }
            }
            catch (TaskCanceledException)
            {
                // 忽略取消异常
            }
        }

        private async Task AnimateColorAsync(Image image, Color from, Color to, float du, CancellationToken token)
        {
            float t = 0f;

            while (t < du)
            {
                if (token.IsCancellationRequested) return;

                float progress = t / du;
                if (image != null)
                    image.color = Color.Lerp(from, to, progress);

                t += Time.deltaTime;
                await Task.Yield();
            }

            if (image != null)
                image.color = to;
        }
    }

}
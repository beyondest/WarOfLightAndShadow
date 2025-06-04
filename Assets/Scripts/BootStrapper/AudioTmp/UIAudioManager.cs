using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SparFlame.BootStrapper
{
    public class UIAudioFeedback : MonoBehaviour
    {
        public AudioClip hoverSound;
        public float hoverVolume = 0.5f;
        public AudioClip clickSound;
        public float clickVolume = 0.5f;
        private AudioSource audioSource;

        void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        void Update()
        {
            if (EventSystem.current == null) return;

            // 鼠标悬停检测
            GameObject obj = EventSystem.current.currentSelectedGameObject;
            GameObject hoverObj = GetUIUnderMouse();

            if (hoverObj && hoverObj.GetComponent<Button>())
            {
                if (lastHover != hoverObj)
                {
                    Play(hoverSound,hoverVolume);
                    lastHover = hoverObj;
                }
            }

            // 鼠标点击检测
            if (Input.GetMouseButtonDown(0))
            {
                GameObject clicked = GetUIUnderMouse();
                if (clicked && clicked.GetComponent<Button>())
                {
                    Play(clickSound,clickVolume);
                }
            }
        }

        GameObject lastHover = null;

        GameObject GetUIUnderMouse()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            foreach (var result in results)
            {
                if (result.gameObject.GetComponent<Button>())
                    return result.gameObject;
            }

            return null;
        }

        void Play(AudioClip clip,float volume = 1f)
        {
            if (clip) audioSource.PlayOneShot(clip,volume);
        }
    }
}
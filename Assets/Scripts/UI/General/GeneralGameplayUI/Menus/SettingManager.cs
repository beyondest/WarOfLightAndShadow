using System;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class SettingManager : MonoBehaviour
    {
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private Toggle hintToggle;

        public Action<bool> OnToggleHintWindow;

        public static SettingManager Instance;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        void Start()
        {
            SetFullscreen(true);
            HintsEnable(true);

            fullscreenToggle.isOn = Screen.fullScreen;
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

            muteToggle.isOn = AudioListener.volume == 0f;
            muteToggle.onValueChanged.AddListener(Mute);

            hintToggle.isOn = true;
            hintToggle.onValueChanged.AddListener(HintsEnable);
        }

        void SetFullscreen(bool isFullscreen)
        {
            Screen.SetResolution(1920, 1080, isFullscreen);
        }

        void Mute(bool isMuted)
        {
            AudioListener.volume = isMuted ? 0f : 1f;
        }

        public void HintsEnable(bool enable)
        {
            OnToggleHintWindow?.Invoke(enable);
        }
    }
}
using UnityEngine;

namespace SparFlame.Systems.General.Audio
{
    [RequireComponent(typeof(AudioListener))]
    public class AudioListenerNoticer : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<AudioListener>() .enabled = false;
        }

        private void OnEnable()
        {
            AudioManager.Instance.EnableGlobalAudioListener(false);
            GetComponent<AudioListener>().enabled = true;
        }

        private void OnDisable()
        {
            GetComponent<AudioListener>().enabled = false;
            AudioManager.Instance.EnableGlobalAudioListener(true);
        }
    }
}
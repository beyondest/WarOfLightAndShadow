using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using UnityEngine;

namespace SparFlame.Systems.General.Audio
{
    [RequireComponent(typeof(AudioListener))]
    public class AudioManager : MonoBehaviour
    {
        
        [TableList, SerializeField]
        private List<AudioClipConfig> audioClips;
        
        // Interface
        public static AudioManager Instance;

        public void PlayAtPosition(AudioName audioName, Vector3 position)
        {
            if(!audioClipDict.TryGetValue(audioName, out var config))
            {
                Debug.LogError("AudioManager: Audio clip not found for name: " + audioName);
                return;
            }
            AudioSource.PlayClipAtPoint(config.clip, position, config.volume);
        }

        public void EnableGlobalAudioListener(bool setEnabled)
        {
            if(!_listener)return;
            _listener.enabled = setEnabled;
        }
        
        // Internal Data
        private readonly Dictionary<AudioName, AudioClipConfig> audioClipDict = new();
        private AudioListener _listener;
        #region EventFunctinos

        

        
        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
            _listener = GetComponent<AudioListener>();
            _listener.enabled = true;
        }
        private void Start()
        {
            foreach (var audioClipConfig in audioClips)
            {
                if (!audioClipDict.TryAdd(audioClipConfig.name, audioClipConfig))
                {
                    Debug.LogError("AudioManager: Duplicate audio clip name found: " + audioClipConfig.name);
                }
            }
        }
 
        #endregion

        
    }
    [Serializable]
    public class AudioClipConfig
    {
        public AudioName name;
        public float volume = 1f;
        public AudioClip clip;
    }
}
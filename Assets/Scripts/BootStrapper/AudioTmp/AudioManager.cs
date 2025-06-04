using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance;
        [TableList]
        public List<AudioClipConfig> audioClips;
        
        private readonly Dictionary<AudioName, AudioClipConfig> audioClipDict = new();
        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
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

        public void PlayAtPosition(AudioName audioName, Vector3 position)
        {
            if(!audioClipDict.TryGetValue(audioName, out var config))
            {
                Debug.LogError("AudioManager: Audio clip not found for name: " + audioName);
                return;
            }
            AudioSource.PlayClipAtPoint(config.clip, position, config.volume);
        }
    }
    [Serializable]
    public class AudioClipConfig
    {
        public AudioName name;
        public float volume = 1f;
        public AudioClip clip;
    }
}
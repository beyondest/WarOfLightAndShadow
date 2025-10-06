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
        [SerializeField] private bool muteAtStart;
        [SerializeField] private float minDistance = 100f;
        [SerializeField] private float maxDistance = 600f;

        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private int expandStep = 5;
        public AudioSource bgm;
        private Queue<AudioSource> availableSources = new Queue<AudioSource>();
        private List<AudioSource> allSources = new List<AudioSource>();
        [TableList, SerializeField] private List<AudioClipConfig> audioClips;

        // Interface
        public static AudioManager Instance;

        public void PlayAtPosition(AudioName audioName, Vector3 position)
        {
            if (!audioClipDict.TryGetValue(audioName, out var config))
            {
                Debug.LogWarning("AudioManager: Audio clip not found for name: " + audioName);
                return;
            }
            PlayClipAtPoint(config.clip, position, config.volume, minDistance, maxDistance);
            // AudioSource.PlayClipAtPoint(config.clip, position, config.volume);
            // CustomAudio.PlayClipAtPoint(config.clip, position,config.volume,minDistance, maxDistance);
            // PlayClipAtPoint(config.clip, position, config.volume, minDistance, maxDistance);
        }

        public void EnableGlobalAudioListener(bool setEnabled)
        {
            if (!_listener) return;
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
            if (muteAtStart)
            {
                AudioListener.volume = 0;
            }
            InitializePool(initialPoolSize);
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

        private void InitializePool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                CreateNewSource();
            }
        }

        private AudioSource CreateNewSource()
        {
            var go = new GameObject("PooledAudioSource");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = false;

            allSources.Add(src);
            availableSources.Enqueue(src);
            return src;
        }

        public static void PlayClipAtPoint(
            AudioClip clip,
            Vector3 position,
            float baseVolume = 1f,
            float minDistance = 1f,
            float maxDistance = 50f,
            AnimationCurve volumeCurve = null)
        {
            if (clip == null) return;

            var pool = Instance;
            AudioSource src = pool.GetAvailableSource();
            pool.ConfigureAndPlay(src, clip, position, baseVolume, minDistance, maxDistance, volumeCurve);
        }

        private AudioSource GetAvailableSource()
        {
            // 优先从可用队列取
            if (availableSources.Count > 0)
            {
                return availableSources.Dequeue();
            }

            // 否则扩容
            InitializePool(expandStep);
            return availableSources.Dequeue();
        }

        private void ConfigureAndPlay(AudioSource src, AudioClip clip, Vector3 position,
            float baseVolume, float minDistance, float maxDistance, AnimationCurve volumeCurve)
        {
            src.transform.position = position;

            Vector3 listenerPos = Camera.main ? Camera.main.transform.position : Vector3.zero;
            float distance = Vector3.Distance(listenerPos, position);
            if (distance > maxDistance)
            {
                ReturnToPool(src);
                return;
            }

            float t = Mathf.InverseLerp(maxDistance, minDistance, distance);
            float volume = (volumeCurve != null ? volumeCurve.Evaluate(t) : t) * baseVolume;
            volume = Mathf.Clamp01(volume);

            src.volume = volume;
            src.clip = clip;
            src.Play();

            StartCoroutine(ReturnAfterPlay(src, clip.length));
        }

        private System.Collections.IEnumerator ReturnAfterPlay(AudioSource src, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(src);
        }

        private void ReturnToPool(AudioSource src)
        {
            src.Stop();
            src.clip = null;
            availableSources.Enqueue(src);
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
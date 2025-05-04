using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Utils;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class GeneralResourceManager : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.3f;

        // Interface

        public static GeneralResourceManager Instance;
        public event Action OnLoadAllResources;
        public event Action<float> OnAllResourceLoaded;
        public event Action<float> OnInitProgress;
        public event Action OnReleaseAllResources;

        public void Register(CustomDs.IResourceManager provider)
        {
            _providers.Add(provider);
            OnLoadAllResources += provider.LoadResources;
            OnReleaseAllResources += provider.UnloadResources;
        }

        public void UnRegister(CustomDs.IResourceManager provider)
        {
            if (_providers.Contains(provider))
            {
                _providers.Remove(provider);
                OnLoadAllResources -= provider.LoadResources;
                OnReleaseAllResources -= provider.UnloadResources;
            }
        }

        public void ReleaseAllResources()
        {
            OnReleaseAllResources?.Invoke();
        }

        public void StartLoadResources()
        {
            OnLoadAllResources?.Invoke();
            StartCoroutine(CheckAllResourceLoadingCoroutine());
        }

        // Internal Data
        private readonly List<CustomDs.IResourceManager> _providers = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GameController.Instance.OnPlayerChooseFaction += _ => OnLoadAllResources?.Invoke();
            GameController.Instance.OnBackToMainMenu += () => OnReleaseAllResources?.Invoke();
        }

        private IEnumerator CheckAllResourceLoadingCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(checkInitInterval);

                var allReady = _providers.All(p => p.IsInitialized);
                if (allReady)
                    break;
                var avgProgress = _providers.Average(p => p.InitProgress);
                OnInitProgress?.Invoke(avgProgress);
            }
            OnAllResourceLoaded?.Invoke(Time.time);
        }
    }
}
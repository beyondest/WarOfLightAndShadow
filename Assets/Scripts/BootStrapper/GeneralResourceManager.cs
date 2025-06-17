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
        public event Action OnAllResourceLoaded;
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
            GameController.Instance.OnPlayerChooseFaction += faction =>
            {
                StartLoadResources();
            };
            GameController.Instance.OnBackToMainMenu += ReleaseAllResources;
        }

        private IEnumerator CheckAllResourceLoadingCoroutine()
        {
            while (true)
            {
                var allReady = _providers.All(p => p.IsInitialized);
                if (allReady)
                    break;
                var avgProgress = _providers.Average(p => p.InitProgress);
                OnInitProgress?.Invoke(avgProgress);
                yield return new WaitForSeconds(checkInitInterval);
            }

            OnAllResourceLoaded?.Invoke();
        }
    }
}
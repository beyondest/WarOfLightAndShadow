using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Core.Interfaces;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GeneralResourceManager : MonoBehaviour
    {
        [SerializeField] private float checkInitInterval = 0.3f;

        // Interface

        public static GeneralResourceManager Instance;
        public event Action OnLoadAllResources;
        public event Action OnAllResourceLoaded;
        public readonly ResourceLoadingUtils.LoadingProgress LoadingProgress = new();
        public event Action OnReleaseAllResources;

        public void Register(IResourceManager provider)
        {
            _providers.Add(provider);
            OnLoadAllResources += provider.LoadResources;
            OnReleaseAllResources += provider.UnloadResources;
        }

        public void UnRegister(IResourceManager provider)
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
        private readonly List<IResourceManager> _providers = new();
        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GameController.Instance.OnPlayerChooseFaction += _ =>
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
                LoadingProgress?.Report(avgProgress);
                yield return new WaitForSeconds(checkInitInterval);
            }

            OnAllResourceLoaded?.Invoke();
        }
        
        
    }
}
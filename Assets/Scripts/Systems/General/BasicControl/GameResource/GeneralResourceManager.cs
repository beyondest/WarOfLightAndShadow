using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class GeneralResourceManager : MonoBehaviour
    {
        public class Operation : ResourceOperation
        {
            private readonly GeneralResourceManager _resourceManager;
            public override float Progress => _resourceManager._initProgress;

            public Operation(IEnumerator routine) : base(routine)
            {
                _resourceManager =Instance;
            }
        }

        [SerializeField] private float checkInitInterval = 0.3f;

        // Interface
        private float _initProgress;
        public static GeneralResourceManager Instance;
        public event Action OnLoadAllResources;
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
            _initProgress = 0f;
            OnReleaseAllResources?.Invoke();
        }

        public Operation LoadResourcesAsync()
        {
            return new Operation(Load());
        }

        private IEnumerator Load()
        {
            OnLoadAllResources?.Invoke();
            while (true)
            {
                var allReady = _providers.All(p => p.IsInitialized);
                if (allReady)
                    break;
                _initProgress = _providers.Average(p => p.InitProgress);
                yield return new WaitForSecondsRealtime(checkInitInterval);
            }
            _initProgress = 1f;
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


       
    }
}
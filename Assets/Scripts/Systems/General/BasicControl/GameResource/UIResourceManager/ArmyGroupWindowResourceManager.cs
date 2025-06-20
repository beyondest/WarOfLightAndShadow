using System;
using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using SparFlame.Core.Interfaces;
using SparFlame.Core.Utils;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class ArmyGroupWindowResourceManager : MonoBehaviour, IResourceManager
    {
        //  Config
        [SerializeField] private string armyGroupIconTypeSuffix = "";
        
        //  Interface
        public static ArmyGroupWindowResourceManager Instance;
        
        public bool IsInitialized => _group.IsHandleCreated() && _group.IsDone;
        public float InitProgress => _group.AverageProgress;

        public readonly Dictionary<ArmyGroupIconType, Sprite> ArmyGroupIcons = new();


        public void LoadResources()
        {
            _group.Add(ResourceLoadingUtils.LoadTypeSuffix<ArmyGroupIconType, Sprite>(
                armyGroupIconTypeSuffix,
                result => ResourceLoadingUtils.OnTypeSuffixLoadComplete(result, ArmyGroupIcons)));
        }

        public void UnloadResources()
        {
            _group.Release();
            ArmyGroupIcons.Clear();
        }

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }

        // Internal Data
        private readonly ResourceLoadingUtils.AddressableResourceGroup _group = new();
    }
}
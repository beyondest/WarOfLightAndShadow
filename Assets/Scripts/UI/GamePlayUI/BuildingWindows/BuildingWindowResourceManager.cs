using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using SparFlame.Utils;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace SparFlame.UI.GamePlay
{
    public class BuildingWindowResourceManager : CustomResourceManager
    {
        [Header("Config")]
        public BuildingDatabaseSo buildingDatabaseSo;

        [SerializeField]
        [CanBeNull] private string buildingTypeSuffix;
        
        
        public static BuildingWindowResourceManager Instance;

        public readonly Dictionary<BuildingType, List<BuildingInfoSpritePair>> BuildingTypeInfoList = new();
        public readonly Dictionary<BuildingType, Sprite> BuildingTypeSprites = new();
        private AddressableResourceGroup _buildingSpritesHandleGroup;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                BuildingTypeInfoList.Add(type, new List<BuildingInfoSpritePair>());
            }

            foreach (var buildingData in buildingDatabaseSo.buildingsData)
            {
                var list = BuildingTypeInfoList[buildingData.buildingType];
                var index = list.Count;
                list.Add(default);
                var handle = CR.LoadAssetRefAsync<Sprite>(buildingData.sprite2D, sprite =>
                {
                    list[index] = new BuildingInfoSpritePair
                    {
                        Subtype = buildingData.GetSubtype(),
                        Sprite = sprite,
                        Tier = buildingData.tier
                    };
                });
                _buildingSpritesHandleGroup.Add(handle);
            }

            var handle1 = CR.LoadTypeSuffix<BuildingType, Sprite>(buildingTypeSuffix,
                result =>
                {
                    CR.OnTypeSuffixLoadComplete(result, BuildingTypeSprites);
                });
            _buildingSpritesHandleGroup.Add(handle1);
        }

        public override bool IsResourceLoaded()
        {
            return _buildingSpritesHandleGroup.IsHandleCreated(buildingDatabaseSo.buildingsData.Count + 1) &&
                   _buildingSpritesHandleGroup.IsDone;
        }

        public struct BuildingInfoSpritePair
        {
            public Sprite Sprite;
            public int Subtype;
            public Tier Tier;
        }

        public void GetFilteredSprites(BuildingType buildingType, List<Sprite> sprites, List<int> saveIndices,
            int subType = -1, Tier tier = Tier.TierNone)
        {
            var list = BuildingTypeInfoList[buildingType];
            for (var i = 0; i < list.Count; i++)
            {
                if (subType != -1 && list[i].Subtype != subType) continue;
                if (tier == Tier.TierNone && list[i].Tier != tier) continue;
                sprites.Add(list[i].Sprite);
                saveIndices.Add(i);
            }
        }
    }
}
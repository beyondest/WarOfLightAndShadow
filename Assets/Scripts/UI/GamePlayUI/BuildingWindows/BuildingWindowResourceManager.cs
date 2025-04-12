using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using SparFlame.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class BuildingWindowResourceManager : CustomResourceManager
    {
        [Header("Config")] public BuildingDatabaseSo buildingDatabaseSo;

        [SerializeField] [CanBeNull] private string buildingTypeSuffix;
        [SerializeField] [CanBeNull] private string functionButtonConjuringTierTypeSuffix;
        [SerializeField] [CanBeNull] private string functionButtonGeneratingTierTypeSuffix;


        // Interface
        public static BuildingWindowResourceManager Instance;
        public readonly Dictionary<BuildingType, Sprite> BuildingTypeSprites = new();
        public readonly Dictionary<Tier, Sprite> FunctionConjuringButtonSprites = new();
        public readonly Dictionary<Tier, Sprite> FunctionGeneratingButtonSprites = new();

        public void GetFilteredBuildingSprites(BuildingType buildingType, List<Sprite> sprites,
            List<Entity> entities, int subType = -1, Tier tier = Tier.TierNone)
        {
            var list = _buildingTypeInfoList[buildingType];
            for (var i = 0; i < list.Count; i++)
            {
                if (subType != -1 && list[i].Subtype != subType) continue;
                if (tier != Tier.TierNone && list[i].Tier != tier) continue;
                sprites.Add(list[i].Sprite);
                entities.Add(list[i].BuildingEntity);
            }
        }

        public override bool IsResourceLoaded()
        {
            return _buildingSpritesHandleGroup.IsHandleCreated(buildingDatabaseSo.buildingsData.Count + 3) &&
                   _buildingSpritesHandleGroup.IsDone;
        }

        // Internal Data
        private readonly AddressableResourceGroup _buildingSpritesHandleGroup = new();
        private readonly Dictionary<BuildingType, List<BuildingInfoSpritePair>> _buildingTypeInfoList = new();
        private EntityManager _em;
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnEnable()
        {
            StartCoroutine(LoadResourceNextFrame());
        }

        private Dictionary<BuildingType, List<Entity>> InitBuildingEntities()
        {
            var buildingDatabase = new Dictionary<BuildingType, List<Entity>>();
            foreach (BuildingType buildingType in Enum.GetValues(typeof(BuildingType)))
            {
                buildingDatabase.Add(buildingType, new List<Entity>());
            }
            var query = _em.CreateEntityQuery(typeof(BuildingSlot));
            var buffer = query.GetSingletonBuffer<BuildingSlot>();
            var bufferEntity = query.GetSingletonEntity();
            foreach (var buildingSlot in buffer)
            {
                var buildingList = buildingDatabase[buildingSlot.Type];
                buildingList.Add(buildingSlot.Entity);
            }
            _em.DestroyEntity(bufferEntity);
            return buildingDatabase;
        }

        private IEnumerator LoadResourceNextFrame()
        {

            yield return null;
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var dict = InitBuildingEntities();
            foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
            {
                _buildingTypeInfoList.Add(type, new List<BuildingInfoSpritePair>());
            }

            foreach (var buildingData in buildingDatabaseSo.buildingsData)
            {
                var list = _buildingTypeInfoList[buildingData.BuildingType];
                var index = list.Count;
                list.Add(default);
                var handle = CR.LoadAssetRefAsync<Sprite>(buildingData.sprite2D, sprite =>
                {
                    list[index] = new BuildingInfoSpritePair
                    {
                        Subtype = buildingData.GetSubtype(),
                        Sprite = sprite,
                        Tier = buildingData.Tier,
                        BuildingEntity = dict[buildingData.BuildingType][index]
                    };
                });
                _buildingSpritesHandleGroup.Add(handle);
            }

            var handle1 = CR.LoadTypeSuffix<BuildingType, Sprite>(buildingTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, BuildingTypeSprites); });
            _buildingSpritesHandleGroup.Add(handle1);
            var handle2 = CR.LoadTypeSuffix<Tier, Sprite>(functionButtonConjuringTierTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, FunctionConjuringButtonSprites); });
            _buildingSpritesHandleGroup.Add(handle2);
            var handle3 = CR.LoadTypeSuffix<Tier, Sprite>(functionButtonGeneratingTierTypeSuffix,
                result => { CR.OnTypeSuffixLoadComplete(result, FunctionGeneratingButtonSprites); });
            _buildingSpritesHandleGroup.Add(handle3);

        }
        
        
        // Here store the sprites resource and entity, with info used to filter entity
        private struct BuildingInfoSpritePair
        {
            public Sprite Sprite;
            public int Subtype;
            public Tier Tier;
            public Entity BuildingEntity;
        }
    }
}
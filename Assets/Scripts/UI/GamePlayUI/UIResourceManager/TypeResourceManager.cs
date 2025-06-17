using System;
using System.Collections;
using System.Collections.Generic;
using GamePlaySystem.Database;
using SparFlame.BootStrapper;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.General;
using SparFlame.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    // Here store the sprite resource and entity, with info used to filter entity
    public struct SpriteEntityInfo
    {
        // For Visualize info of building for a construct window
        public string GameplayName;
        public Sprite Sprite;

        // For fast filter
        public int SubtypeIndex;
        public Tier Tier;
        public FactionTag FactionTag; 

        // For pass the entity from ui to ecs
        public Entity EntityPrefab;
    }

    public class TypeResourceManager<TEnum, TData, TEntityPrefabData> : MonoBehaviour,CustomDs.IResourceManager
        where TEnum : Enum
        where TData : GeneralDataItem
        where TEntityPrefabData : unmanaged, IEntityPrefabData<TEnum>
    {

        public float loadResourceTimeOutSeconds = 10f;
        // Interface
        public virtual bool IsResourceLoaded()
        {
            return ResourceGroup.IsHandleCreated() && ResourceGroup.IsDone;
        }

        public SpriteEntityInfo GetInfoByGeneralTypeAndIdx(TEnum type, int globalIdx)
        {
            return _typeInfos[type][globalIdx];
        }

        public List<SpriteEntityInfo> GetFilteredInfoList(TEnum type,FactionTag factionTag = default,
            int subType = 0, Tier tier = Tier.Tier1, bool filterFaction = true, bool filterSubType = false, bool filterTier = false)
        {
            var dict = _typeInfos[type];
            var infos = new List<SpriteEntityInfo>();

            foreach (var pair in dict)
            {
                if(filterFaction && pair.Value.FactionTag != factionTag) continue;
                if (filterSubType && pair.Value.SubtypeIndex != subType) continue;
                if (filterTier && pair.Value.Tier != tier) continue;
                infos.Add(pair.Value);
            }

            return infos;
        }

        // Cache
        private readonly Dictionary<TEnum, Dictionary<int, SpriteEntityInfo>> _typeInfos = new();

        // Internal Data
        protected readonly AddressableResourceGroup ResourceGroup = new();
        private EntityManager _em;
        private float _elapsedTime;

        private void Start()
        {
            GeneralResourceManager.Instance.Register(this);
        }


        public virtual void LoadResources()
        {
            StartCoroutine(LoadSpriteEntityInfo());
        }

        public virtual void UnloadResources()
        {
            ResourceGroup.Release();
            _typeInfos.Clear();
        }


        private Dictionary<TEnum, List<Entity>> InitEntities()
        {
            var entities = new Dictionary<TEnum, List<Entity>>();
            foreach (TEnum type in Enum.GetValues(typeof(TEnum)))
            {
                entities.Add(type, new List<Entity>());
            }

            var query = _em.CreateEntityQuery(typeof(TEntityPrefabData));
            var buffer = query.GetSingletonBuffer<TEntityPrefabData>();
            // var bufferEntity = query.GetSingletonEntity();
            foreach (var buildingSlot in buffer)
            {
                var typeList = entities[buildingSlot.Type];
                typeList.Add(buildingSlot.Prefab);
            }

            // _em.DestroyEntity(bufferEntity);
            return entities;
        }

        // Wait until subscene loaded
        private IEnumerator LoadSpriteEntityInfo()
        {
            while (World.DefaultGameObjectInjectionWorld == null)
            {
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime >= loadResourceTimeOutSeconds)
                    throw new ArgumentException(
                        $"Resource Manager : {nameof(TData)} wait for entity world time out of {loadResourceTimeOutSeconds} seconds.)");
                yield return null;
            }

            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            while (true)
            {
                var query = _em.CreateEntityQuery(typeof(TEntityPrefabData));
                if (!query.IsEmptyIgnoreFilter)
                    break;
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime >= loadResourceTimeOutSeconds)
                    throw new ArgumentException(
                        $"Resource manager : {nameof(TData)} wait for entity query time out of {loadResourceTimeOutSeconds} seconds.)");
                yield return null;
            }

            var databaseSo = DatabaseManager.GetDatabaseSo<TData>();
            var dict = InitEntities();
            foreach (TEnum type in Enum.GetValues(typeof(TEnum)))
            {
                _typeInfos.Add(type, new Dictionary<int, SpriteEntityInfo>());
            }

            foreach (var dataItem in databaseSo.Items)
            {
                TEnum type = (TEnum)Enum.ToObject(typeof(TEnum), dataItem.GetGeneralTypeIndex());
                var subDict = _typeInfos[type];
                var entityIndex = subDict.Count;
                if (!subDict.TryAdd(dataItem.id, new SpriteEntityInfo()))
                {
                    throw new ArgumentException(
                        $"Failed to load sprites by database {databaseSo.name}, id {dataItem.id} duplicated");
                }

                var handle = CR.LoadAssetRefAsync<Sprite>(dataItem.sprite2D, sprite =>
                {
                    subDict[dataItem.id] = new SpriteEntityInfo
                    {
                        Sprite = sprite,
                        GameplayName = dataItem.gameplayName,
                        Tier = dataItem.curTier,
                        SubtypeIndex = dataItem.GetSubtypeIndex(),
                        EntityPrefab = dict[type][entityIndex],
                        FactionTag = dataItem.factionTag,
                    };
                });
                ResourceGroup.Add(handle);
            }
        }

        public bool IsInitialized => IsResourceLoaded();
        public float InitProgress => ResourceGroup.AverageProgress;
    }
}
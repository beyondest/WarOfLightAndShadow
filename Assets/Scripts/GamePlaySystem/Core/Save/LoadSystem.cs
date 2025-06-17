using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.GamePlaySystem.Save
{

  
    
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct LoadSystem : ISystem
    {
        private NativeHashMap<int, Entity> _globalIdxToPrefabs;
        private NativeHashMap<long, Entity> _tmpIdxToInstances;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;

        private ComponentLookup<SeInGarrison> _inGarrisonLookup;
        private ComponentLookup<SeTmpId> _tmpIdLookup;
        private ComponentLookup<PhysicsMass> _physicsMassLookup;
        private ComponentLookup<SeInverseMass> _inverseMassLookup;
        private ComponentLookup<ExpData> _expDataLookup;
        
        
        private BufferLookup<SeGarrisonEntity> _garrisonEntitiesLookup;
        private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SaveConfig>();
            state.RequireForUpdate<LoadData>();
            _inGarrisonLookup = state.GetComponentLookup<SeInGarrison>(true);
            _inverseMassLookup = state.GetComponentLookup<SeInverseMass>(true);
            _tmpIdLookup = state.GetComponentLookup<SeTmpId>(true);
            _physicsMassLookup = state.GetComponentLookup<PhysicsMass>(true);
            _expDataLookup = state.GetComponentLookup<ExpData>(true);
            
            _garrisonEntitiesLookup = state.GetBufferLookup<SeGarrisonEntity>(true);
            _garrisonTypeDataLookup = state.GetBufferLookup<GarrisonTypeData>(true);
        }

        public void OnUpdate(ref SystemState state)
        {
            _expDataLookup.Update(ref state);
            _inverseMassLookup.Update(ref state);
            _physicsMassLookup.Update(ref state);
            _tmpIdLookup.Update(ref state);
            _inGarrisonLookup.Update(ref state);
            
            _garrisonEntitiesLookup.Update(ref state);
            _garrisonTypeDataLookup.Update(ref state);
            
            if (!_globalIdxToPrefabs.IsCreated)
            {
                Initialize();
            }
            var loadRequestData = SystemAPI.GetSingleton<LoadData>();
            var config = SystemAPI.GetSingleton<SaveConfig>();
            switch (loadRequestData.Type)
            {
                case SaveLoadType.None:
                    return;
                case SaveLoadType.OnlyCity:
                    var ecb = new EntityCommandBuffer(Allocator.TempJob);
                    using (var deserializeWorld = new World("Deserialization World"))
                    {
                        var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                        using (var reader = new StreamBinaryReader(SaveUtilities.GetCitySavePath(loadRequestData.CityId,config )))
                        {
                            SerializeUtility.DeserializeWorld(transaction, reader);
                        }
                        deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                        state.EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                        
                    }
                    var loadJob = new LoadSubGameplayJob
                    {
                        ECB = ecb.AsParallelWriter(),
                        GarrisonEntitiesLookup = _garrisonEntitiesLookup,
                        GarrisonTypeDataLookup = _garrisonTypeDataLookup,
                        InGarrisonLookup = _inGarrisonLookup,
                        TmpIdLookup = _tmpIdLookup,
                        PhysicsMassLookup = _physicsMassLookup,
                        GlobalIdxToPrefabs = _globalIdxToPrefabs,
                        InverseMassLookup = _inverseMassLookup,
                        ExpLookup = _expDataLookup,
                    }.ScheduleParallel(state.Dependency);
                    loadJob.Complete();
                    ecb.Playback(state.EntityManager);
                    ecb.Dispose();
                    foreach (var (tmpId, entity) in SystemAPI.Query<RefRO<SeTmpId>>().WithEntityAccess())
                    {
                        _tmpIdxToInstances.Add(tmpId.ValueRO.value,entity);
                    }
                    _garrisonEntitiesLookup.Update(ref state);
                    _inGarrisonLookup.Update(ref state);
                    var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
                    var replaceJob = new ReplaceTmpIdJob
                    {
                        ECB = ecb2.AsParallelWriter(),
                        SeGarrisonEntitiesLookup = _garrisonEntitiesLookup,
                        SeInGarrisonLookup = _inGarrisonLookup,
                        TmpIdxToInstances = _tmpIdxToInstances,
                    }.ScheduleParallel(state.Dependency);
                    replaceJob.Complete();
                    ecb2.Playback(state.EntityManager);
                    ecb2.Dispose();
                    _tmpIdxToInstances.Clear();
                    
                    SystemAPI.SetSingleton(new LoadData
                    {
                        Type = SaveLoadType.None,
                        CityId = -1
                    });
                    break;
                case SaveLoadType.OnlyMainGameplay:
                    break;
                case SaveLoadType.AllFromCity:
                    break;
                case SaveLoadType.AllFromWildBattle:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Initialize()
        {
            var buildingDatabase = SystemAPI.GetSingletonBuffer<BuildingEntityPrefabData>();
            var unitDatabase = SystemAPI.GetSingletonBuffer<UnitEntityPrefabData>();
            var expDatabase = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            _globalIdxToPrefabs = new NativeHashMap<int, Entity>(buildingDatabase.Length + unitDatabase.Length, Allocator.Persistent);
            foreach (var prefabData in buildingDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.GlobalIdx, prefabData.Prefab);
            }

            foreach (var prefabData in unitDatabase)
            {
                _globalIdxToPrefabs.Add(prefabData.GlobalIdx, prefabData.Prefab);
            }
            _expDatabase = new NativeHashMap<int, ExpStaticConfig>(expDatabase.Length, Allocator.Persistent);
            foreach (var expData in expDatabase)
            {
                _expDatabase.Add(expData.GlobalIdx, expData);
            }

            _tmpIdxToInstances = new NativeHashMap<long, Entity>(100, allocator: Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if(_globalIdxToPrefabs.IsCreated)
                _globalIdxToPrefabs.Dispose();
            if(_tmpIdxToInstances.IsCreated)
                _tmpIdxToInstances.Dispose();
            if(_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }
        
    }
}
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LoadSystemPlus : SystemBase
    {
        private NativeHashMap<int, Entity> _globalIdxToPrefabs;
        private NativeHashMap<long, Entity> _tmpIdxToInstances;
        private NativeHashMap<int, ExpStaticConfig> _expDatabase;

        // private ComponentLookup<SeInGarrison> _inGarrisonLookup;
        // private ComponentLookup<SeTmpId> _tmpIdLookup;
        // private ComponentLookup<PhysicsMass> _physicsMassLookup;
        // private ComponentLookup<SeInverseMass> _inverseMassLookup;
        // private ComponentLookup<ExpData> _expDataLookup;
        //
        //
        // private BufferLookup<SeGarrisonEntity> _garrisonEntitiesLookup;
        // private BufferLookup<GarrisonTypeData> _garrisonTypeDataLookup;
        
        private ComponentLookup<VolumeObstacleSpawnRequest> _volumeObstacleSpawnRequestsLookup;

        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<SaveConfig>();
            RequireForUpdate<LoadData>();
            RequireForUpdate<BuildingEntityPrefabData>();
            RequireForUpdate<UnitEntityPrefabData>();
            RequireForUpdate<PlayerSaveSlot>();
            // _inGarrisonLookup = GetComponentLookup<SeInGarrison>(true);
            // _inverseMassLookup = GetComponentLookup<SeInverseMass>(true);
            // _tmpIdLookup = GetComponentLookup<SeTmpId>(true);
            // _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            // _expDataLookup = GetComponentLookup<ExpData>(true);
            //
            // _garrisonEntitiesLookup = GetBufferLookup<SeGarrisonEntity>(true);
            // _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _volumeObstacleSpawnRequestsLookup = GetComponentLookup<VolumeObstacleSpawnRequest>(true);
        }

        protected override void OnDestroy()
        {
            if (_globalIdxToPrefabs.IsCreated)
                _globalIdxToPrefabs.Dispose();
            if (_tmpIdxToInstances.IsCreated)
                _tmpIdxToInstances.Dispose();
            if (_expDatabase.IsCreated)
                _expDatabase.Dispose();
        }

        protected override void OnStartRunning()
        {
            if (!_initialized)
            {
                Initialize();
                GameController.Instance.OnEcsLoadCitySaving += LoadCity;
                _initialized = true;
            }
            
        }

        protected override void OnUpdate()
        {
        }

        private void LoadCity(int cityId)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            using (var deserializeWorld = new World("Deserialization World"))
            {
                var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                using (var reader =
                       new StreamBinaryReader(SaveUtilities.GetCitySavePath(cityId,playerSaveSlot)))
                {
                    SerializeUtility.DeserializeWorld(transaction, reader);
                }
                
                deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
            }
            // _expDataLookup.Update(this);
            // _inverseMassLookup.Update(this);
            // _physicsMassLookup.Update(this);
            // _tmpIdLookup.Update(this);
            // _inGarrisonLookup.Update(this);
            //
            // _garrisonEntitiesLookup.Update(this);
            // _garrisonTypeDataLookup.Update(this);
            _volumeObstacleSpawnRequestsLookup.Update(this);
            var job =new BuildingLoadJob
            {
                ECB = ecb.AsParallelWriter(),
                Prefabs = _globalIdxToPrefabs,
                RequestLookup = _volumeObstacleSpawnRequestsLookup,
            }.ScheduleParallel(Dependency);
            job.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            /*
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
            }.ScheduleParallel(Dependency);
            loadJob.Complete();
            ecb.Playback(EntityManager);
            ecb.Dispose();
            foreach (var (tmpId, entity) in SystemAPI.Query<RefRO<SeTmpId>>().WithEntityAccess())
            {
                _tmpIdxToInstances.Add(tmpId.ValueRO.value, entity);
            }

            _garrisonEntitiesLookup.Update(this);
            _inGarrisonLookup.Update(this);
            var ecb2 = new EntityCommandBuffer(Allocator.TempJob);
            var replaceJob = new ReplaceTmpIdJob
            {
                ECB = ecb2.AsParallelWriter(),
                SeGarrisonEntitiesLookup = _garrisonEntitiesLookup,
                SeInGarrisonLookup = _inGarrisonLookup,
                TmpIdxToInstances = _tmpIdxToInstances,
            }.ScheduleParallel(Dependency);
            replaceJob.Complete();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            _tmpIdxToInstances.Clear();
            */

            SystemAPI.SetSingleton(new LoadData
            {
                Type = SaveLoadType.None,
                CityId = -1
            });
        }

        private void Initialize()
        {
            var buildingDatabase = SystemAPI.GetSingletonBuffer<BuildingEntityPrefabData>();
            var unitDatabase = SystemAPI.GetSingletonBuffer<UnitEntityPrefabData>();
            var expDatabase = SystemAPI.GetSingletonBuffer<ExpStaticConfig>();
            _globalIdxToPrefabs =
                new NativeHashMap<int, Entity>(buildingDatabase.Length + unitDatabase.Length, Allocator.Persistent);
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
        [WithNone(typeof(VolumeObstacleSpawnRequest))]
        [WithAll(typeof(BuildingAttr))]
        [WithNone(typeof(ConstructingData))]
        public partial struct BuildingLoadJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int, Entity> Prefabs;
            [ReadOnly] public ComponentLookup<VolumeObstacleSpawnRequest> RequestLookup;
            private void Execute([ChunkIndexInQuery]int index,in SubGameplayGeneralAttr generalAttr, Entity selfEntity)
            {
                var prefab = Prefabs[generalAttr.ID];
                var request = RequestLookup[prefab];
                ECB.AddComponent(index, selfEntity, request);
                ECB.SetComponentEnabled<VolumeObstacleSpawnRequest>(index, selfEntity, true);
            }
        }
    }
}
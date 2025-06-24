using System.IO;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.Systems.General.BasicControl
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LoadSystemPlus : SystemBase
    {
        private EntityQuery _loadArmyGroupQuery;

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

        private ComponentLookup<VolumeObstacleSpawnRequest> _volumeObstacleSpawnRequestsLookup;

        private bool _initialized;


        protected override void OnCreate()
        {
            RequireForUpdate<SaveLoadConfig>();
            RequireForUpdate<BuildingEntityPrefabData>();
            RequireForUpdate<UnitEntityPrefabData>();
            RequireForUpdate<PlayerSaveSlot>();
            _inGarrisonLookup = GetComponentLookup<SeInGarrison>(true);
            _inverseMassLookup = GetComponentLookup<SeInverseMass>(true);
            _tmpIdLookup = GetComponentLookup<SeTmpId>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            _expDataLookup = GetComponentLookup<ExpData>(true);
            
            _garrisonEntitiesLookup = GetBufferLookup<SeGarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
            _volumeObstacleSpawnRequestsLookup = GetComponentLookup<VolumeObstacleSpawnRequest>(true);

            _loadArmyGroupQuery = SystemAPI.QueryBuilder().WithAll<InSubGameTag>().WithAll<ArmyGroupAttr>().Build();
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
                _initialized = true;
                SaveLoadController.Instance.OnEcsLoadCitySubData += LoadCitySubData;
                SaveLoadController.Instance.OnEcsLoadArmyGroupSubData += LoadArmyGroupSubData;
            }
        }

        protected override void OnUpdate()
        {
        }

        private void LoadArmyGroupSubData()
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var armyGroupAttrs = _loadArmyGroupQuery.ToComponentDataArray<ArmyGroupAttr>(Allocator.Temp);
            foreach (var armyGroupAttr in armyGroupAttrs)
            {
                var armyGroupPath = SaveUtilities.GetArmyGroupSavePath(armyGroupAttr.SaveId, playerSaveSlot);
                if(!File.Exists(armyGroupPath))continue;
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(armyGroupPath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }

            armyGroupAttrs.Dispose();
        }

        private void LoadCitySubData(SubGameStatusData targetSubGameStatusData)
        {
            var city = targetSubGameStatusData.City;
            var cityAttr = SystemAPI.GetComponent<CityAttr>(city);
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var citySavePath = SaveUtilities.GetCitySavePath(cityAttr.ID, playerSaveSlot);

            // Load city data
            var fileExist = File.Exists(citySavePath);
            if (fileExist)
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(citySavePath))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }

                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }
           

            _expDataLookup.Update(this);
            _inverseMassLookup.Update(this);
            _physicsMassLookup.Update(this);
            _tmpIdLookup.Update(this);
            _inGarrisonLookup.Update(this);
            
            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            
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
            var replaceTmpRefJob = new ReplaceTmpIdJob
            {
                ECB = ecb2.AsParallelWriter(),
                SeGarrisonEntitiesLookup = _garrisonEntitiesLookup,
                SeInGarrisonLookup = _inGarrisonLookup,
                TmpIdxToInstances = _tmpIdxToInstances,
            }.ScheduleParallel(Dependency);
            replaceTmpRefJob.Complete();
            ecb2.Playback(EntityManager);
            ecb2.Dispose();
            _tmpIdxToInstances.Clear();
            
            
            var ecb3 = new EntityCommandBuffer(Allocator.TempJob);
            _volumeObstacleSpawnRequestsLookup.Update(this);
            var buildingSpawnObstacleJob = new BuildingSpawnObstacleJob
            {
                ECB = ecb3.AsParallelWriter(),
                Prefabs = _globalIdxToPrefabs,
                RequestLookup = _volumeObstacleSpawnRequestsLookup,
            }.ScheduleParallel(Dependency);
            buildingSpawnObstacleJob.Complete();
            ecb3.Playback(EntityManager);
            ecb3.Dispose();

         
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
        public partial struct BuildingSpawnObstacleJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public NativeHashMap<int, Entity> Prefabs;
            [ReadOnly] public ComponentLookup<VolumeObstacleSpawnRequest> RequestLookup;

            private void Execute([ChunkIndexInQuery] int index, in SubGameplayGeneralAttr generalAttr,
                Entity selfEntity)
            {
                var prefab = Prefabs[generalAttr.ID];
                var request = RequestLookup[prefab];
                ECB.AddComponent(index, selfEntity, request);
                ECB.SetComponentEnabled<VolumeObstacleSpawnRequest>(index, selfEntity, true);
            }
        }
    }
}
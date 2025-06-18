using SparFlame.Components.SubGameplay;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Physics;

namespace SparFlame.Systems.General.BasicControl
{
    public partial class LoadSystemPlus : SystemBase
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

        private bool _initialized;
        protected override void OnCreate()
        {
            RequireForUpdate<SaveConfig>();
            RequireForUpdate<LoadData>();
            _inGarrisonLookup = GetComponentLookup<SeInGarrison>(true);
            _inverseMassLookup = GetComponentLookup<SeInverseMass>(true);
            _tmpIdLookup = GetComponentLookup<SeTmpId>(true);
            _physicsMassLookup = GetComponentLookup<PhysicsMass>(true);
            _expDataLookup = GetComponentLookup<ExpData>(true);

            _garrisonEntitiesLookup = GetBufferLookup<SeGarrisonEntity>(true);
            _garrisonTypeDataLookup = GetBufferLookup<GarrisonTypeData>(true);
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
                GameController.Instance.EcsLoadCitySaving += LoadCity;
                _initialized = true;
            }
            
        }

        protected override void OnUpdate()
        {
        }

        private void LoadCity(int cityId)
        {
            _expDataLookup.Update(this);
            _inverseMassLookup.Update(this);
            _physicsMassLookup.Update(this);
            _tmpIdLookup.Update(this);
            _inGarrisonLookup.Update(this);
            
            _garrisonEntitiesLookup.Update(this);
            _garrisonTypeDataLookup.Update(this);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            using (var deserializeWorld = new World("Deserialization World"))
            {
                var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                using (var reader =
                       new StreamBinaryReader(SaveUtilities.GetCitySavePath(cityId)))
                {
                    SerializeUtility.DeserializeWorld(transaction, reader);
                }

                deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
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
    }
}
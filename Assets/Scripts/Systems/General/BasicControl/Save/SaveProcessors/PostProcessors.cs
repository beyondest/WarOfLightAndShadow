using System.Threading.Tasks;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl
{
    #region ArmyGroupSubData

    public class ArmyGroupSubDataPostProcessorArgs
    {
        public EntityManager Em;
        public JobHandle Dependency;
        public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;

        public ArmyGroupSubDataPostProcessorArgs(EntityManager em, JobHandle dependency,
            ComponentLookup<ArmyGroupAttr> armyGroupAttrLookup)
        {
            Em = em;
            Dependency = dependency;
            ArmyGroupAttrLookup = armyGroupAttrLookup;
        }
    }

    [BurstCompile]
    [WithNone(typeof(FakeUnitTag))]
    public partial struct ArmyGroupUnitPostProcessJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> Map;
        [ReadOnly] public ComponentLookup<ArmyGroupAttr> ArmyGroupAttrLookup;

        private void Execute([ChunkIndexInQuery] int index, ref InArmyGroup inArmyGroup, ref LocalTransform transform,
            in FormationTransform formationTransform,
            Entity selfEntity)
        {
            inArmyGroup.BelongsTo = Map[inArmyGroup.SingleId];
            var armyGroupAttr = ArmyGroupAttrLookup[inArmyGroup.BelongsTo];
            var position = formationTransform.Transform.Position;
            position = armyGroupAttr.loadingCenter + armyGroupAttr.loadingScale * position;
            transform.Position = position;
            ECB.AddComponent<SubGameplayEntityTag>(index, selfEntity);
            ECB.RemoveComponent<AssignGlobalSingleIDRequest>(index, selfEntity);
            ECB.RemoveComponent<AssignRandomRequest>(index, selfEntity);
        }
    }

    [BurstCompile]
    [WithAll(typeof(InSubGameTag))]
    public partial struct ArmyGroupReplaceUnitIdJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Entity> Map;

        private void Execute(ref DynamicBuffer<ArmyGroupUnit> units)
        {
            for (var i = 0; i < units.Length; i++)
            {
                var unit = units[i];
                unit.Unit = Map[unit.SingleId];
                units[i] = unit;
            }
        }
    }

    public class ArmyGroupSubDataPostProcessor : ISavePostProcessor
    {
        public Task Run(object args)
        {
            var ag = (ArmyGroupSubDataPostProcessorArgs)args;
            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var tmpIdxToInstances = new NativeHashMap<long, Entity>(100, Allocator.Persistent);

            using var loadedUnitQuery = ag.Em.CreateEntityQuery(typeof(InArmyGroup), typeof(GlobalSingleId));
            using var loadedUnits = loadedUnitQuery.ToEntityArray(Allocator.Persistent);
            using var unitSingleIds = loadedUnitQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            for (var i = 0; i < loadedUnits.Length; i++)
            {
                tmpIdxToInstances.Add(unitSingleIds[i].value, loadedUnits[i]);
            }

            using var inSubGameArmyGroupQuery = ag.Em.CreateEntityQuery(typeof(InSubGameTag), typeof(GlobalSingleId),
                typeof(ArmyGroupAttr));
            using var armyGroups = inSubGameArmyGroupQuery.ToEntityArray(Allocator.Persistent);
            using var armyGroupSingleIds =
                inSubGameArmyGroupQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);

            for (var i = 0; i < armyGroups.Length; i++)
            {
                tmpIdxToInstances.Add(armyGroupSingleIds[i].value, armyGroups[i]);
            }

            var ecbP = ecb.AsParallelWriter();
            var job1 = new ArmyGroupUnitPostProcessJob
            {
                Map = tmpIdxToInstances,
                ArmyGroupAttrLookup = ag.ArmyGroupAttrLookup,
                ECB = ecbP
            }.ScheduleParallel(ag.Dependency);

            var job2 = new ArmyGroupReplaceUnitIdJob
            {
                Map = tmpIdxToInstances
            }.ScheduleParallel(ag.Dependency);
            ag.Dependency = JobHandle.CombineDependencies(job1, job2);
            ag.Dependency.Complete();
            ecb.Playback(ag.Em);
            ecb.Dispose();
            tmpIdxToInstances.Dispose();
            return Task.CompletedTask;
        }
    }

    #endregion

    #region GameMainData

    public class GameMainDataPostProcessorArgs
    {
        public EntityManager Em;
        public JobHandle Dependency;
        public ComponentLookup<EnemyArmyGroupBelongsToCity> EnemyBelongsToCityLookup;

        public GameMainDataPostProcessorArgs(EntityManager em, JobHandle dependency,
            ComponentLookup<EnemyArmyGroupBelongsToCity> enemyBelongsToCityLookup)
        {
            Em = em;
            Dependency = dependency;
            EnemyBelongsToCityLookup = enemyBelongsToCityLookup;
        }
    }

    public class GameMainDataPostProcessor : ISavePostProcessor
    {
        public Task Run(object args)
        {
            return Task.CompletedTask;
        }
    }

    [BurstCompile]
    public partial struct ArmyGroupPostProcessJob : IJobEntity
    {
        [ReadOnly] public ArmyGroupBillboardConfig BillboardConfig;
        [ReadOnly] public NativeHashMap<long, Entity> Map;
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<EnemyArmyGroupBelongsToCity> ArmyGroupBelongsToCityLookup;
        [ReadOnly] public ComponentLookup<ArmyGroupInGarrison> ArmyGroupInGarrisonLookup;

        private void Execute(
            [ChunkIndexInQuery] int index, in ArmyGroupAttr armyGroupAttr,
            in DynamicBuffer<LinkedEntityGroup> children,
            in ArmyGroupMovableData movableData,
            ref LastPassingByCity lastPassingByCity, Entity selfEntity,
            ref ArmyGroupStateData stateData
        )
        {
            if (ArmyGroupInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                inGarrison.City = Map[inGarrison.SingleId];
                ECB.SetComponent(index, selfEntity, inGarrison);
            }
            if(stateData.TargetSingleId != 0)
                stateData.Target = Map[stateData.TargetSingleId];
            if(lastPassingByCity.SingleId != 0)
                lastPassingByCity.City = Map[lastPassingByCity.SingleId];
            if (movableData.movementInfo == ArmyGroupMovementInfo.NotComplete)
            {
                ECB.SetComponentEnabled<ArmyGroupMovingTag>(index, selfEntity, true);
            }
            
            if (ArmyGroupBelongsToCityLookup.TryGetComponent(selfEntity, out var belongsToCity))
            {
                belongsToCity.City = Map[belongsToCity.SingleId];
            }

            var billboard = children[BillboardConfig.IconChildIndex];
            var selectedIcon = children[BillboardConfig.SelectChildIndex];
            ECB.SetComponent(index, billboard.Value,
                new ArmyGroupBillboardImageIDFloatOverride { Value = (int)armyGroupAttr.iconType });
            ECB.AddComponent<DisableRendering>(index,selectedIcon.Value);
            
            
            ECB.AddComponent<MainGameplayEntityTag>(index, selfEntity);
            ECB.RemoveComponent<AssignGlobalSingleIDRequest>(index, selfEntity);
        }
    }

    [BurstCompile]
    public partial struct CityPostProcessJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Entity> Map;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index,
            ref DynamicBuffer<CityGarrisonEntity> garrisonEntities,
            ref DynamicBuffer<AttackArmyGroup> attackArmyGroups,
            ref DynamicBuffer<DefendArmyGroup> defendArmyGroups,
            ref DynamicBuffer<ExtraArmyGroup> extraArmyGroups,
            ref DynamicBuffer<InvadingArmyGroup> invadingArmyGroups,
            ref DynamicBuffer<InvadeTarget> invadeTargets,
            ref DynamicBuffer<CityFutureInvaders> cityFutureInvaders,
            Entity selfEntity)
        {
            ECB.AddComponent<MainGameplayEntityTag>(index, selfEntity);
            SetEntityBySingleId(ref garrisonEntities);
            SetEntityBySingleId(ref attackArmyGroups);
            SetEntityBySingleId(ref defendArmyGroups);
            SetEntityBySingleId(ref extraArmyGroups);
            SetEntityBySingleId(ref invadingArmyGroups);
            SetEntityBySingleId(ref cityFutureInvaders);
            for (var i = 0; i < invadeTargets.Length; i++)
            {
                var target = invadeTargets[i];
                target.City = Map[target.SingleId];
                invadeTargets[i] = target;
            }

            ECB.RemoveComponent<AssignGlobalSingleIDRequest>(index, selfEntity);
            ECB.RemoveComponent<AssignRandomRequest>(index, selfEntity);
        }

        private void SetEntityBySingleId<T>(ref DynamicBuffer<T> garrisonEntities)
            where T : unmanaged, ICityArmyGroupElement, IBufferElementData
        {
            for (var i = garrisonEntities.Length - 1; i >= 0; i--)
            {
                var cityGarrisonEntity = garrisonEntities[i];
                if (!Map.TryGetValue(cityGarrisonEntity.SingleId, out var armyGroup))
                {
                    garrisonEntities.RemoveAt(i);
                    continue;
                }
                cityGarrisonEntity.ArmyGroup =armyGroup;
                garrisonEntities[i] = cityGarrisonEntity;
            }
        }
    }

    #endregion

    #region CitySubData

    public class CitySubDataPostProcessorArgs
    {
        public EntityManager Em;
        public JobHandle Dependency;
        public BufferLookup<ConjuringData> ConjuringDataLookup;
        public ComponentLookup<InGarrison> InGarrisonLookup;

        public CitySubDataPostProcessorArgs(EntityManager em, JobHandle dependency,
            BufferLookup<ConjuringData> conjuringDataLookup, ComponentLookup<InGarrison> inGarrisonLookup)
        {
            Em = em;
            Dependency = dependency;
            ConjuringDataLookup = conjuringDataLookup;
            InGarrisonLookup = inGarrisonLookup;
        }
    }

    public class CitySubDataPostProcessor : ISavePostProcessor
    {
        public Task Run(object args)
        {
            var ag = (CitySubDataPostProcessorArgs)args;
            using var cityGarrisonUnitQuery = ag.Em.CreateEntityQuery(typeof(GlobalSingleId), typeof(InGarrison));
            using var cityGarrisonBuildingQuery = ag.Em.CreateEntityQuery(typeof(GlobalSingleId), typeof(GarrisonAttr));
            using var units = cityGarrisonUnitQuery.ToEntityArray(Allocator.Persistent);
            using var buildings = cityGarrisonBuildingQuery.ToEntityArray(Allocator.Persistent);
            using var unitSingleIds = cityGarrisonUnitQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            using var buildingSingleIds =
                cityGarrisonBuildingQuery.ToComponentDataArray<GlobalSingleId>(Allocator.Persistent);
            var tmpIdxToInstances = new NativeHashMap<long, Entity>(100, Allocator.Persistent);
            for (var i = 0; i < units.Length; i++)
            {
                tmpIdxToInstances.Add(unitSingleIds[i].value, units[i]);
            }

            for (var i = 0; i < buildings.Length; i++)
            {
                tmpIdxToInstances.Add(buildingSingleIds[i].value, buildings[i]);
            }

            var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var ecbP = ecb.AsParallelWriter();
            var job1 = new CityUnitPostProcessJob
            {
                InGarrisonLookup = ag.InGarrisonLookup,
                Map = tmpIdxToInstances,
                ECB = ecbP
            }.Schedule(ag.Dependency);

            var job2 = new CityBuildingPostProcessJob
            {
                Map = tmpIdxToInstances,
                ECB = ecbP,
                ConjuringDataLookup = ag.ConjuringDataLookup
            }.Schedule(ag.Dependency);
            ag.Dependency = JobHandle.CombineDependencies(job1, job2);
            ag.Dependency.Complete();
            ecb.Playback(ag.Em);
            ecb.Dispose();
            tmpIdxToInstances.Dispose();

            return Task.CompletedTask;
        }
    }

    [BurstCompile]
    [WithAll(typeof(UnitAttr))]
    [WithNone(typeof(InArmyGroup))]
    public partial struct CityUnitPostProcessJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Entity> Map;
        [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute(
            [ChunkIndexInQuery] int index, Entity selfEntity)
        {
            if (InGarrisonLookup.HasComponent(selfEntity))
            {
                var inGarrison = InGarrisonLookup[selfEntity];
                inGarrison.BuildingEntity = Map[inGarrison.SingleId];
                ECB.SetComponent(index, selfEntity, inGarrison);
                ECB.SetComponentEnabled<GarrisonStateTag>(index, selfEntity, true);
                ECB.SetComponent(index, selfEntity, new BasicStateData
                {
                    TargetEntity = Entity.Null,
                    CurState = InteractState.Garrison,
                    TargetState = InteractState.Idle
                });
            }

            ECB.AddComponent<SubGameplayEntityTag>(index, selfEntity);
            ECB.RemoveComponent<AssignGlobalSingleIDRequest>(index, selfEntity);
            ECB.RemoveComponent<AssignRandomRequest>(index, selfEntity);
        }
    }

    [BurstCompile]
    [WithAll(typeof(BuildingAttr))]
    public partial struct CityBuildingPostProcessJob : IJobEntity
    {
        [ReadOnly] public NativeHashMap<long, Entity> Map;
        [ReadOnly] public NativeHashMap<int, Entity> PrefabDatabase;
        public EntityCommandBuffer.ParallelWriter ECB;
        // This is non-parallel job
        [NativeDisableParallelForRestriction] public BufferLookup<ConjuringData> ConjuringDataLookup;
        [NativeDisableParallelForRestriction] public BufferLookup<GarrisonEntity> GarrisonEntitiesLookup;

        private void Execute(
            [ChunkIndexInQuery] int index,
            Entity selfEntity)
        {
            if (GarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                for (int i = 0; i < garrisonEntities.Length; i++)
                {
                    var garrisonEntity = garrisonEntities[i];
                    garrisonEntity.Unit = Map[garrisonEntity.SingleId];
                    garrisonEntities[i] = garrisonEntity;
                }
            }

            if (ConjuringDataLookup.TryGetBuffer(selfEntity, out var buffer))
            {
                ECB.AddComponent<ConjuringTag>(index, selfEntity);
                for (var i = buffer.Length - 1; i >= 0; i--)
                {
                    var data = buffer[i];
                    data.ConjuringEntity = PrefabDatabase[data.PrefabId];
                    buffer[i] = data;
                }
            }

            ECB.AddComponent<SubGameplayEntityTag>(index, selfEntity);
            ECB.RemoveComponent<AssignGlobalSingleIDRequest>(index, selfEntity);
        }
    }

    #endregion
}
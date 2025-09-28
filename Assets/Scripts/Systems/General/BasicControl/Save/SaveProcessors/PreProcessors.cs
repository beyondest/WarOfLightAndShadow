using System.Threading.Tasks;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl
{
    #region CityUnit

    public class CityUnitSavePreArgs
    {
        public JobHandle Dependency;
        public ComponentLookup<NeedSaveTag> NeedSaveTagLookup;

        public CityUnitSavePreArgs(ComponentLookup<NeedSaveTag> needSaveTagLookup, JobHandle dependency)
        {
            NeedSaveTagLookup = needSaveTagLookup;
            Dependency = dependency;
        }
    }

    public class CityUnitSavePreProcessor : ISavePreProcessor
    {
        public Task Run(object args
        )
        {
            return Task.CompletedTask;
        }
    }

    [BurstCompile]
    [WithAll(typeof(UnitAttr))]
    [WithNone(typeof(InArmyGroup))]
    public partial struct CityUnitSetNeedSaveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.SetComponentEnabled<NeedSaveTag>(index, selfEntity, true);
        }
    }

    #endregion


    #region CityBuilding

    public class CityBuildingSavePreProcessorArgs
    {
        public JobHandle Dependency;
        public ComponentLookup<NeedSaveTag> NeedSaveTagLookup;

        public CityBuildingSavePreProcessorArgs(ComponentLookup<NeedSaveTag> needSaveTagLookup,
            JobHandle dependency)
        {
            Dependency = dependency;
            NeedSaveTagLookup = needSaveTagLookup;
        }
    }

    public class BuildingSavePreProcessor : ISavePreProcessor
    {
        public Task Run(object args)
        {
            return Task.CompletedTask;
        }
    }

    [BurstCompile]
    [WithAll(typeof(BuildingAttr))]
    public partial struct CityBuildingSetNeedSaveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.SetComponentEnabled<NeedSaveTag>(index, selfEntity, true);
        }
    }

    #endregion

    #region ArmyGroupUnit

    public class ArmyGroupUnitPreProcessorArgs
    {
        public Entity ArmyGroup;
        public EntityCommandBuffer ECB;
        public EntityManager Em;

        public ArmyGroupUnitPreProcessorArgs
        (
            Entity armyGroup,
            EntityCommandBuffer ecb,
            EntityManager em)
        {
            ArmyGroup = armyGroup;
            ECB = ecb;
            Em = em;
        }
    }

    public class ArmyGroupUnitPreProcessor : ISavePreProcessor
    {
        public Task Run(object args)
        {
            var ag = (ArmyGroupUnitPreProcessorArgs)args;
            if (ag.Em.HasComponent<EnemyArmyGroupSaveTag>(ag.ArmyGroup))
                ag.ECB.RemoveComponent<EnemyArmyGroupSaveTag>(ag.ArmyGroup);

            var sum = float3.zero;
            var armyGroupUnits = ag.Em.GetBuffer<ArmyGroupUnit>(ag.ArmyGroup);

            for (var j = 0; j < armyGroupUnits.Length; j++)
            {
                var transform = ag.Em.GetComponentData<LocalTransform>(armyGroupUnits[j].Unit);
                sum += transform.Position;
            }

            var center = sum / armyGroupUnits.Length;
            float2 boundingMin = float2.zero, boundingMax = float2.zero;
            foreach (var armyGroupUnit in armyGroupUnits)
            {
                var unit = armyGroupUnit.Unit;
                var transform = ag.Em.GetComponentData<LocalTransform>(unit);
                var relative = transform.Position - center;
                boundingMin = math.min(boundingMin, relative.xz);
                boundingMax = math.max(boundingMax, relative.xz);
                transform.Position = relative;
                ag.ECB.SetComponent(unit, new FormationTransform { Transform = transform });
                ag.ECB.SetComponentEnabled<NeedSaveTag>(unit, true);
            }

            var armyGroupAttr = ag.Em.GetComponentData<ArmyGroupAttr>(ag.ArmyGroup);
            // Record the bounding box
            armyGroupAttr.boundingBoxDelta = boundingMax - boundingMin;
            armyGroupAttr.loadingCenter = center;
            armyGroupAttr.loadingScale = 1f;
            ag.ECB.SetComponent(ag.ArmyGroup, armyGroupAttr);
            return Task.CompletedTask;
        }
    }

    #endregion

    #region GameMain

    public class GameMainDataPreProcessorArgs
    {
    }

    public class CitySavePreProcessorArgs
    {
        public ComponentLookup<NeedSaveTag> NeedSaveTagLookup;
        public JobHandle Dependency;

        public CitySavePreProcessorArgs(ComponentLookup<NeedSaveTag> needSaveTagLookup, JobHandle dependency)
        {
            NeedSaveTagLookup = needSaveTagLookup;
            Dependency = dependency;
        }
    }

    public class ArmyGroupSavePreProcessorArgs
    {
        public ComponentLookup<NeedSaveTag> NeedSaveTagLookup;
        public JobHandle Dependency;

        public ArmyGroupSavePreProcessorArgs(ComponentLookup<NeedSaveTag> needSaveTagLookup, JobHandle dependency)
        {
            NeedSaveTagLookup = needSaveTagLookup;
            Dependency = dependency;
        }
    }

    public class GameMainSavePreProcessor : ISavePreProcessor
    {
        public Task Run(object args)
        {
            return Task.CompletedTask;
        }
    }

    public class CitySavePreProcessor : ISavePreProcessor
    {
        public Task Run(object args)
        {
            return Task.CompletedTask;
        }
    }

    public class ArmyGroupSavePreProcessor : ISavePreProcessor
    {
        public Task Run(object args)
        {
            return Task.CompletedTask;
        }
    }

    [BurstCompile]
    [WithAll(typeof(ArmyGroupAttr))]
    public partial struct MainGameplayArmyGroupSetNeedSaveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.SetComponentEnabled<NeedSaveTag>(index, selfEntity, true);
        }
    }

    [BurstCompile]
    [WithAll(typeof(CityAttr))]
    public partial struct MainGameplayCitySetNeedSaveTagJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int index, Entity selfEntity)
        {
            ECB.SetComponentEnabled<NeedSaveTag>(index, selfEntity, true);
        }
    }

    #endregion
}
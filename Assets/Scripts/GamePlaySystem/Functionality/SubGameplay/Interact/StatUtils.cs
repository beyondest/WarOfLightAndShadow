using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.PopNumber;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    public struct StatUtils
    {

        public static void GenerateRemoveFromTeamRequest(EntityCommandBuffer.ParallelWriter ecb, int index, Entity interacteeEntity, InTeamTag inTeamTag,
            in UnitAttr unitAttr)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new RemoveFromTeamRequest
            {
                UnitAttr =unitAttr,
                BelongsToTeam = inTeamTag.BelongsToTeam,
                UnitToRemove = interacteeEntity
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
        }

        public static void GenerateHarvestResourceRequest(in StatChangeRequest request,
            in SubGameplayGeneralAttr interactorAttr,
            in ResourceAttr resourceAttr, int index, EntityCommandBuffer.ParallelWriter ecb,
            int absAmount)
        {
            var entity = ecb.CreateEntity(index);
            ecb.AddComponent(index, entity, new ResourceChangeRequest
            {
                Type = resourceAttr.Type,
                AbsAmount = absAmount,
                FromFaction = interactorAttr.FactionTag,
                RequestType = ResourceRequestType.Harvest
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, entity);
        }

        public static void GeneratePopNumberRequest(ref ComponentLookup<LocalTransform> transformLookup,
            in StatChangeRequest request,
            in SubGameplayGeneralAttr subGameplayGeneralAttr, int index, EntityCommandBuffer.ParallelWriter ecb,
            int absAmount)
        {
            if(absAmount <= 0)return;
            var interactorFaction = subGameplayGeneralAttr.FactionTag;
            var popNumberType = (request.Type, interactorFaction) switch
            {
                (StatChangeType.Heal, FactionTag.Ally) => PopNumberType.AllyHealed,
                (StatChangeType.Attack, FactionTag.Ally) => PopNumberType.DamageDealt,
                (StatChangeType.Heal, FactionTag.Enemy) => PopNumberType.EnemyHealed,
                (StatChangeType.Attack, FactionTag.Enemy) => PopNumberType.DamageTaken,
                (StatChangeType.Harvest, FactionTag.Ally) => PopNumberType.AllyHarvest,
                (StatChangeType.Harvest, FactionTag.Enemy) => PopNumberType.EnemyHarvest,
                _ => PopNumberType.UnNormalKill
            };

            var interacteePos = transformLookup[request.Interactee].Position;
            // Spawn Pop Number VFX
            var popNumberRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, popNumberRequest, new PopNumberRequest
            {
                ColorId = (int)popNumberType,
                Position = interacteePos,
                Scale = 1f,
                Value = absAmount
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, popNumberRequest);
        }

        public static void GenerateDestroyObstacleRequest(Entity interacteeEntity, bool isResource,
            int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var destroyObstacleRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, destroyObstacleRequest, new VolumeObstacleDestroyRequest
            {
                FromEntity = interacteeEntity,
                RequestFromFaction = isResource ? FactionTag.Neutral : FactionTag.Ally // Ally or enemy is both ok
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, destroyObstacleRequest);
        }


        public static void GenerateGarrisonUnitDieRequest(Entity interacteeEntity, int index,
            in SubGameplayGeneralAttr interacteeAttr,
            ref ComponentLookup<InGarrison> inGarrisonLookup, EntityCommandBuffer.ParallelWriter ecb)
        {
            if (!inGarrisonLookup.TryGetComponent(interacteeEntity, out var inGarrison)) return;
            var garrisonUnitDieRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, garrisonUnitDieRequest, new GarrisonUnitDieRequest
            {
                BuildingEntity = inGarrison.BuildingEntity,
                UnitEntity = interacteeEntity,
                Id = interacteeAttr.ID
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, garrisonUnitDieRequest);
        }

        public static void GenerateReleasePopulationRequest(Entity interacteeEntity, int index,
            in SubGameplayGeneralAttr interacteeAttr,
            ref BufferLookup<CostList> costListLookup, EntityCommandBuffer.ParallelWriter ecb)
        {
            var releasePopulationRequest = ecb.CreateEntity(index);
            var costList = costListLookup[interacteeEntity];
            var amount = 0;
            foreach (var cost in costList)
            {
                if (cost.Type == ResourceType.SoulPact) amount = cost.Amount;
            }

            ecb.AddComponent(index, releasePopulationRequest, new ResourceChangeRequest
            {
                AbsAmount = math.abs(amount),
                FromFaction = interacteeAttr.FactionTag,
                RequestType = ResourceRequestType.Release,
                Type = ResourceType.SoulPact
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, releasePopulationRequest);
        }




        public static void GenerateDwellingDestroyResourceChangeRequest(Entity interacteeEntity, int index,
            in SubGameplayGeneralAttr interacteeAttr, in DwellingAttr dwellingAttr, EntityCommandBuffer.ParallelWriter ecb)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new ResourceChangeRequest
            {
                Type = dwellingAttr.ResourceType,
                AbsAmount = math.abs(dwellingAttr.Amount),
                FromFaction = interacteeAttr.FactionTag,
                RequestType = ResourceRequestType.DwellingDestroyConsume
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
        }
    }
}
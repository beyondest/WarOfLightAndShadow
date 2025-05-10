using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Hints;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.PopNumber;
using SparFlame.GamePlaySystem.Resource;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    public struct StatUtils
    {
        public static void GenerateResourceTierNotMatchHint(ref ComponentLookup<LocalTransform> transformLookup,
            in StatChangeRequest request, int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var entity = ecb.CreateEntity(index);
            ecb.AddComponent(index, entity, new HintRequest
            {
                Position = transformLookup[request.Interactee].Position
            });
            ecb.AddComponent<GameplayEntityTag>(index,entity);
        }

        public static void GenerateHarvestResourceRequest(in StatChangeRequest request,
            in GeneralAttr interactorAttr,
            in ResourceAttr resourceAttr, int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var entity = ecb.CreateEntity(index);
            ecb.AddComponent(index, entity, new ResourceChangeRequest
            {
                Type = resourceAttr.Type,
                AbsAmount = math.abs(request.AbsAmount),
                FromFaction = interactorAttr.FactionTag,
                RequestType = ResourceRequestType.Harvest
            });
            ecb.AddComponent<GameplayEntityTag>(index, entity);
        }

        public static void GeneratePopNumberRequest(ref ComponentLookup<LocalTransform> transformLookup,
            in StatChangeRequest request,
            in GeneralAttr generalAttr, int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var popNumberType = PopNumberType.DamageDealt;
            var interactorFaction = generalAttr.FactionTag;
            if (!request.KillByUnNormal)
            {
                popNumberType = (request.InteractType, interactorFaction) switch
                {
                    (InteractType.Heal, FactionTag.Ally) => PopNumberType.AllyHealed,
                    (InteractType.Attack, FactionTag.Ally) => PopNumberType.DamageDealt,
                    (InteractType.Heal, FactionTag.Enemy) => PopNumberType.EnemyHealed,
                    (InteractType.Attack, FactionTag.Enemy) => PopNumberType.DamageTaken,
                    (InteractType.Harvest, FactionTag.Ally) => PopNumberType.AllyHarvest,
                    (InteractType.Harvest, FactionTag.Enemy) => PopNumberType.EnemyHarvest,
                    _ => popNumberType
                };
            }
            else
            {
                popNumberType =  PopNumberType.UnNormalKill;
            }
           
            var interacteePos = transformLookup[request.Interactee].Position;
            // Spawn Pop Number VFX
            var popNumberRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, popNumberRequest, new PopNumberRequest
            {
                ColorId = (int)popNumberType,
                Position = interacteePos,
                Scale = 1f,
                Value = request.AbsAmount
            });
            ecb.AddComponent<GameplayEntityTag>(index, popNumberRequest);

        }

        public static void GenerateDestroyObstacleRequest(Entity interacteeEntity, bool isResource,
            int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var destroyObstacleRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, destroyObstacleRequest, new VolumeObstacleDestroyRequest
            {
                FromEntity = interacteeEntity,
                RequestFromFaction = isResource? FactionTag.Neutral : FactionTag.Ally   // Ally or enemy is both ok
            });
            ecb.AddComponent<GameplayEntityTag>(index, destroyObstacleRequest);

        }
        
        
        public static void GenerateGarrisonUnitDieRequest(Entity interacteeEntity, int index,in GeneralAttr interacteeAttr,
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
            ecb.AddComponent<GameplayEntityTag>(index, garrisonUnitDieRequest);

        }

        public static void GenerateReleasePopulationRequest(Entity interacteeEntity, int index,in GeneralAttr interacteeAttr,
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
            ecb.AddComponent<GameplayEntityTag>(index,releasePopulationRequest);
        }
        
        public static void GenerateChangeOccupiedTagRequest(in GeneralAttr interacteeAttr,  float3 crystalPos,
            EntityCommandBuffer.ParallelWriter ecb,int index)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index,request, new ChangeOccupiedTagRequest
            {
                CrystalFaction = interacteeAttr.FactionTag,
                CrystalPos = crystalPos,
                IsDestroyed = true
            });
            ecb.AddComponent<GameplayEntityTag>(index,request);

        }
        
        
        public static void GenerateDwellingDestroyResourceChangeRequest(Entity interacteeEntity, int index,
            in GeneralAttr interacteeAttr,in DwellingAttr dwellingAttr, EntityCommandBuffer.ParallelWriter ecb)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new ResourceChangeRequest
            {
                Type = dwellingAttr.ResourceType,
                AbsAmount = math.abs(dwellingAttr.Amount),
                FromFaction = interacteeAttr.FactionTag,
                RequestType = ResourceRequestType.DwellingDestroyConsume
            });
            ecb.AddComponent<GameplayEntityTag>(index,request);

        }

    }
}
using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Interact
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

        // public static void GenerateHarvestResourceRequest(in StatChangeRequest request,
        //     in SubGameplayGeneralAttr interactorAttr,
        //     in ResourceAttr resourceAttr, int index, EntityCommandBuffer.ParallelWriter ecb,
        //     int absAmount)
        // {
        //     var entity = ecb.CreateEntity(index);
        //     ecb.AddComponent(index, entity, new ResourceChangeRequest
        //     {
        //         ResourceType = resourceAttr.Type,
        //         AbsAmount = absAmount,
        //         FromFaction = interactorAttr.Faction,
        //         RequestType = ResourceRequestType.Harvest
        //     });
        //     ecb.AddComponent<SubGameplayEntityTag>(index, entity);
        // }

        public static void GeneratePopNumberRequest(ref ComponentLookup<LocalTransform> transformLookup,
            in StatChangeRequest request,
            in SubGameplayGeneralAttr subGameplayGeneralAttr, int index, EntityCommandBuffer.ParallelWriter ecb,
            int absAmount)
        {
            if(absAmount <= 0)return;
            var interactorFaction = subGameplayGeneralAttr.Faction;
            var popNumberType = (request.Type, interactorFaction) switch
            {
                (StatChangeType.Heal, FactionTag.Light) => PopNumberType.AllyHealed,
                (StatChangeType.Attack, FactionTag.Light) => PopNumberType.DamageDealt,
                (StatChangeType.Heal, FactionTag.Dark) => PopNumberType.EnemyHealed,
                (StatChangeType.Attack, FactionTag.Dark) => PopNumberType.DamageTaken,
                (StatChangeType.Harvest, FactionTag.Light) => PopNumberType.AllyHarvest,
                (StatChangeType.Harvest, FactionTag.Dark) => PopNumberType.EnemyHarvest,
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
                RequestFromFaction = isResource ? FactionTag.Neutral : FactionTag.Light // Ally or enemy is both ok
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, destroyObstacleRequest);
        }

        public static void GenerateResourceTaskRemoveRequest(
            int index, 
            int uniqueId,
            Entity city,
            ResourceRequestType requestType,
            EntityCommandBuffer.ParallelWriter ecb
            )
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new ResourceChangeRequest
            {
                AbsAmount = 0,
                RequestType = requestType,
                FromBuildingUniqueId = uniqueId,
                City = city,
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
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
                Id = interacteeAttr.PrefabID
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, garrisonUnitDieRequest);
        }

        public static void GenerateReleasePopulationRequest(Entity interacteeEntity, int index,
            Entity city,
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
                City = city,
                RequestType = ResourceRequestType.PopulationRelease,
                ResourceType = ResourceType.SoulPact,
                
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, releasePopulationRequest);
        }




        public static void GenerateDecreaseStorageRequest( int index,
            Entity city,
            ResourceType resourceType,
            int decreaseAmount,
            EntityCommandBuffer.ParallelWriter ecb)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new ResourceChangeRequest
            {
                ResourceType = resourceType,
                AbsAmount = decreaseAmount,
                City = city,
                RequestType =ResourceRequestType.DecreaseStorage,
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
        }

        public static void GenerateDecreaseGenerateSpeedRequest(int index,
            Entity city, ResourceType resourceType, float hoursPerUnit,
            EntityCommandBuffer.ParallelWriter ecb)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent(index, request, new ResourceChangeRequest
            {
                ResourceType = resourceType,
                HoursPerUnit = hoursPerUnit,
                City = city,
                RequestType = ResourceRequestType.DecreaseGenerateSpeed,
            });
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
        }

        public static void GenerateRemoveFromArmyGroupRequest(Entity requestInteractee,
            int unitId,
            int index, InArmyGroup inArmyGroup, EntityCommandBuffer.ParallelWriter ecb)
        {
            var request = ecb.CreateEntity(index);
            ecb.AddComponent<SubGameplayEntityTag>(index, request);
            ecb.AddComponent(index, request, new RemoveFromArmyGroupRequest
            {
                ArmyGroup = inArmyGroup.BelongsTo,
                Unit = requestInteractee,
                RemoveType = RemoveFromArmyGroupType.RemoveSpecifiedUnitWithoutRemovingInArmyGroup,
                MoveOutId = unitId
            });
        }
    }
}
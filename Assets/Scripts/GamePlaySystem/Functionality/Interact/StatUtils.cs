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
                Type = HintType.ResourceTierNotMatch,
                Position = transformLookup[request.Interactee].Position
            });
        }

        public static void GenerateHarvestResourceRequest(in StatChangeRequest request,
            in InteractableAttr interactorAttr,
            in ResourceAttr resourceAttr, int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var entity = ecb.CreateEntity(index);
            ecb.AddComponent(index, entity, new HarvestResourceRequest
            {
                Type = resourceAttr.Type,
                HarvestAmount = math.abs(request.Amount),
                FromFaction = interactorAttr.FactionTag
            });
        }


        public static void GeneratePopNumberRequest(ref ComponentLookup<LocalTransform> transformLookup,
            in StatChangeRequest request,
            in InteractableAttr interactableAttr, int index, EntityCommandBuffer.ParallelWriter ecb)
        {
            var popNumberType = PopNumberType.DamageDealt;
            var interactorFaction = interactableAttr.FactionTag;
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
            var interacteePos = transformLookup[request.Interactee].Position;

            // Spawn Pop Number VFX
            var popNumberRequest = ecb.CreateEntity(index);
            ecb.AddComponent(index, popNumberRequest, new PopNumberRequest
            {
                ColorId = (int)popNumberType,
                Position = interacteePos,
                Scale = 1f,
                Value = request.Amount
            });
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
        }
    }
}
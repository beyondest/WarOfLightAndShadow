using SparFlame.Components.General;
using SparFlame.Components.VFX;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
    public struct BuffUtils
    {
        public static void AddDamageReduceBuff(EntityCommandBuffer.ParallelWriter ecb,
            int index, Entity targetEntity, FactionTag faction,
            float keepDuration, float elapsedTime)
        {
            ecb.AddComponent(index, targetEntity, new DamageReduceShieldBuff
            {
                StopTime = keepDuration + elapsedTime,
            });
            var vfxRequest = ecb.CreateEntity(index);
            ecb.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
            ecb.AddComponent(index, vfxRequest, new VFXRequest
            {
                Filter = new VFXSubFilter
                {
                    FactionFilterEnable = true,
                    Faction = faction
                },
                RequestType = VFXRequestType.Spawn,
                VFXName = VFXName.CavalryMoveDamageReduction,
                SpawnPosition = float3.zero,
                VFXTrackTarget = targetEntity,
                KeepDuration = keepDuration,
            });
        }

        public static void AddBlessingBuff(EntityCommandBuffer.ParallelWriter ecb,
            int index, Entity targetEntity, Entity aoePrefab, float keepDuration, float elapsedTime,
            Tier tier)
        {
            ecb.AddComponent(index, targetEntity, new AoeTriggerRequest
            {
                Prefab = aoePrefab
            });
            ecb.AddBuffer<AoeTarget>(index, targetEntity);
            ecb.AddComponent(index, targetEntity, new BlessingBuff
            {
                StopTime = elapsedTime + keepDuration,
            });
            var vfxRequest = ecb.CreateEntity(index);
            ecb.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
            ecb.AddComponent(index, vfxRequest, new VFXRequest
            {
                Filter = new VFXSubFilter
                {
                    Tier = tier,
                    TierFilterEnable = true
                },
                RequestType = VFXRequestType.Spawn,
                VFXName = VFXName.BlessingBuff,
                SpawnPosition = float3.zero,
                VFXTrackTarget = targetEntity,
                KeepDuration = keepDuration,
            });
        }

        public static void AddLightShieldBuff(EntityCommandBuffer.ParallelWriter ecb, int index,
            Entity targetEntity, Entity aoePrefab, float keepDuration, float elapsedTime)
        {
            ecb.AddComponent(index, targetEntity, new AoeTriggerRequest
            {
                Prefab = aoePrefab
            });
            ecb.AddBuffer<AoeTarget>(index, targetEntity);
            ecb.AddComponent(index, targetEntity, new LightShieldBuff
            {
                StopTime = elapsedTime + keepDuration,
            });
        }

        public static void AddDarkShieldBuff(EntityCommandBuffer.ParallelWriter ecb, int index, Entity targetEntity,
            float keepDuration, float elapsedTime)
        {
            ecb.AddComponent(index, targetEntity, new DarkShieldTauntBuff
            {
                StopTime = elapsedTime + keepDuration,
            });
        }

        public static void AddDarkCavalryBuff(EntityCommandBuffer.ParallelWriter ECB, int index,
            Entity targetEntity, float keepDuration, float elapsedTime, Tier tier)
        {
            ECB.AddComponent(index, targetEntity, new DarkCavalryBuff
            {
                StopTime = elapsedTime + keepDuration,
            });
            
            var vfxRequest = ECB.CreateEntity(index);
            ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
            ECB.AddComponent(index, vfxRequest, new VFXRequest
            {
                Filter = new VFXSubFilter
                {
                    TierFilterEnable = true,
                    Tier = tier
                },
                KeepDuration = keepDuration,
                RequestType = VFXRequestType.Spawn,
                VFXName = VFXName.DarkCavalryAttackGain,
                SpawnPosition =float3.zero,
                VFXTrackTarget = targetEntity
            });
        }
    }
}
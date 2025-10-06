using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.SubGameplay.Interact;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Systems.SubGameplay.ArmyGroupRealTimeControl
{
    public struct EnemyArcherSkillSettingRequest : IComponentData
    {
        public Entity ArmyGroup;
        public Tier ArrowRainTier;
    }
    public partial struct EnemyArcherSkillSettingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ArcherSkillGeneralConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EnemyArcherSkillSettingRequest>();
            state.RequireForUpdate<EnemyArrowRainTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var enemyArrowRainTarget = SystemAPI.GetSingletonEntity<EnemyArrowRainTarget>();
            var targetTransform = SystemAPI.GetComponent<LocalTransform>(enemyArrowRainTarget);
            var buffer = SystemAPI.GetSingletonBuffer<ArcherSKillConfigs>();
            var request = SystemAPI.GetSingleton<EnemyArcherSkillSettingRequest>();
            SystemAPI.SetSingleton(new EnemyArcherSkill
            {
                Fire = true,
                TargetPosition = targetTransform.Position
            });
            GenerateVfxAndAoeInteractBuff(ref state, buffer[(int)request.ArrowRainTier - 3],targetTransform.Position);
            state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<EnemyArcherSkillSettingRequest>());
        }

         private void GenerateVfxAndAoeInteractBuff(ref SystemState state,in ArcherSKillConfigs config,
            in float3 position)
        {
            var request = SystemAPI.GetSingleton<EnemyArcherSkillSettingRequest>();
            var time = SystemAPI.GetSingleton<GameTimeData>();
            var playerFaction = SystemAPI.GetSingleton<PlayerFactionData>().faction;
            var generalConfig = SystemAPI.GetSingleton<ArcherSkillGeneralConfig>();
            using var ecb = new EntityCommandBuffer(Allocator.Persistent);
            var units = SystemAPI.GetBuffer<ArmyGroupUnit>(request.ArmyGroup);
            var totalPosition = float3.zero;
            var validCount = 0;
            foreach (var unit in units)
            {
                if(!SystemAPI.HasComponent<LocalTransform>(unit.Unit))continue;
                var transform = SystemAPI.GetComponent<LocalTransform>(unit.Unit);
                totalPosition += transform.Position;
                validCount++;
            }

            if (validCount == 0)return;
            totalPosition/= validCount;
            var dis = math.length(totalPosition.xz - position.xz);
            var flightTime =dis / generalConfig.ArrowFlightSpeed ;
            var statChangeRequest = new StatChangeRequest
            {
                Interactor = units[0].Unit,
                Interactee = Entity.Null,
                AbsAmount = 0,
                DamageType = DamageType.Physical,
                InteractorSubGameplayGeneralAttr = new SubGameplayGeneralAttr
                {
                    Faction = ~playerFaction,
                    BaseTag = BaseTag.Units,
                    SubFaction = SubFactionTag.None
                },
                Type = StatChangeType.Attack
            };
          
            var aoeBuffRequest = ecb.CreateEntity();
            ecb.AddComponent<SubGameplayEntityTag>( aoeBuffRequest);
            ecb.AddComponent( aoeBuffRequest, new AoeInteractData
            {
                TriggerTime = time.ElapsedTime + flightTime + generalConfig.TriggerTimeBias,
                StatChangeRequest = statChangeRequest,
                TargetFaction = playerFaction,
                AllFactionTarget = true,
                RandomAbsAmount = true,
                AbsAmountRange = config.DamageRange
            });
            ecb.AddComponent( aoeBuffRequest, new BuffRequest
            {
                SpawnPosition = position,
                SpawnRotation = quaternion.identity,
                TrackTarget = Entity.Null,
                Name = BuffName.ArrowRain,
                Filter = new BuffFilter
                {
                    tier = request.ArrowRainTier,
                    tierFilterEnabled = true
                },
            });
            ecb.Playback(state.EntityManager);
        }

    }
}
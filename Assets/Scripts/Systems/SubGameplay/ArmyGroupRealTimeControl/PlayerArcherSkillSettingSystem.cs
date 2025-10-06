using SparFlame.Components.General;
using SparFlame.Components.Input;
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
    public struct PlayerArcherSkillSettingRequest : IComponentData
    {
        public Tier ArrowRainTier;
        public Entity ArmyGroup;
    }


    public partial struct PlayerArcherSkillSettingSystem : ISystem
    {
        private struct ProgressData : IComponentData
        {
            public Entity IndicatorEntity;
            public bool IsCreated;
            public bool ShouldDestroy;
            public float DestroyedTime;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<InputCastSkillData>();
            state.RequireForUpdate<ArcherSkillGeneralConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<ProgressData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<PlayerArcherSkillSettingRequest>();
            state.EntityManager.CreateSingleton(new ProgressData());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var request = SystemAPI.GetSingleton<PlayerArcherSkillSettingRequest>();
            var mouseData = SystemAPI.GetSingleton<InputMouseData>();
            var progressData = SystemAPI.GetSingleton<ProgressData>();
            var configs = SystemAPI.GetSingletonBuffer<ArcherSKillConfigs>();
            var config = configs[(int)request.ArrowRainTier - 3];
            var inputData = SystemAPI.GetSingleton<InputCastSkillData>();
            if (progressData.ShouldDestroy)
            {
                progressData.ShouldDestroy = false;
                progressData.IsCreated = false;
                state.EntityManager.DestroyEntity(progressData.IndicatorEntity);
                progressData.IndicatorEntity = Entity.Null;
                state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerArcherSkillSettingRequest>());
                SystemAPI.SetSingleton(new PlayerArcherSkill());
                SystemAPI.SetSingleton(progressData);
                return;
            }

            if (!progressData.IsCreated)
            {
                progressData.IndicatorEntity =
                    state.EntityManager.Instantiate(config.IndicatorPrefab);
                progressData.IsCreated = true;
                SystemAPI.SetSingleton(progressData);
            }

            var rotation =
                SystemAPI.HasComponent<TerrainTag>(mouseData.HitEntity) && math.lengthsq(mouseData.HitNormal) > 0.001f
                    ? quaternion.LookRotationSafe(math.forward(), mouseData.HitNormal)
                    : quaternion.identity;
            var transform = SystemAPI.GetComponent<LocalTransform>(progressData.IndicatorEntity);
            transform.Position = mouseData.HitPosition;
            transform.Rotation = rotation;
            SystemAPI.SetComponent(progressData.IndicatorEntity, transform);
            SystemAPI.SetSingleton(new PlayerArcherSkill
            {
                Fire = inputData.Cast,
                TargetPosition = mouseData.HitPosition,
            });
            if (inputData.Cast)
            {
                progressData.ShouldDestroy = true;
                SystemAPI.SetSingleton(progressData);
                GenerateVfxAndAoeInteractBuff(ref state, config, mouseData.HitPosition);
            }
            else if (inputData.Cancel)
            {
                progressData.ShouldDestroy = true;
                SystemAPI.SetSingleton(progressData);
            }
        }

        private void GenerateVfxAndAoeInteractBuff(ref SystemState state,in ArcherSKillConfigs config,
            in float3 position)
        {
            var request = SystemAPI.GetSingleton<PlayerArcherSkillSettingRequest>();
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
                    Faction = playerFaction,
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
                TargetFaction = ~playerFaction,
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
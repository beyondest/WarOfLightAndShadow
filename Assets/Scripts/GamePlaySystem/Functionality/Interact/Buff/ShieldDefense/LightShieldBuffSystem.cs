using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact.ShieldDefense
{
    public partial struct LightShieldBuffSystem : ISystem
    {
        private BufferLookup<LightShieldDefenderData> _defenderData;
        private ComponentLookup<BasicStateData> _basicStateData;
        private ComponentLookup<ExpData> _expLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<GameTimeData>();
            _defenderData = state.GetBufferLookup<LightShieldDefenderData>();
            _basicStateData = state.GetComponentLookup<BasicStateData>(true);
            _expLookup = state.GetComponentLookup<ExpData>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _defenderData.Update(ref state);
            _basicStateData.Update(ref state);
            _expLookup.Update(ref state);
            var job = new ShieldBuffJob
            {
                DefenderLookup = _defenderData,
                StateData = _basicStateData,
                CurTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
                ExpData = _expLookup,
            }.Schedule(state.Dependency);
            job.Complete();

            new DefenderDataCheckApplyVFXJob
            {
                ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                    .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                ExpLookup = _expLookup,
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct DefenderDataCheckApplyVFXJob : IJobEntity
        {
            [ReadOnly] public ComponentLookup<ExpData> ExpLookup;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<LightShieldDefenderData> datas,
                Entity selfEntity,
                in LocalTransform transform, in GeneralAttr generalAttr)
            {
                for (var i = datas.Length - 1; i >= 0; i--)
                {
                    var data = datas[i];
                    if (!ExpLookup.HasComponent(data.Entity))
                    {
                        datas.RemoveAt(i);
                    }
                }

                if (datas.Length > 0)
                {
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        Filter = new VFXSubFilter
                        {
                            FactionFilterEnable = true,
                            Faction = generalAttr.FactionTag
                        },
                        KeepDuration = float.MaxValue,
                        SpawnPosition = transform.Position,
                        TargetPosition = transform.Position,
                        StatChangeRequest = default,
                        RequestType = VFXRequestType.Spawn,
                        VFXName = VFXName.LightShield,
                        VFXTrackTarget = selfEntity
                    });
                }
                else
                {
                    var shieldVfx = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, shieldVfx);
                    ECB.AddComponent(index, shieldVfx, new VFXRequest
                    {
                        Filter = default,
                        KeepDuration = float.MaxValue,
                        SpawnPosition = transform.Position,
                        TargetPosition = transform.Position,
                        StatChangeRequest = default,
                        RequestType = VFXRequestType.Kill,
                        VFXName = VFXName.LightShield,
                        VFXTrackTarget = selfEntity
                    });
                }
            }
        }


        [BurstCompile]
        public partial struct ShieldBuffJob : IJobEntity
        {
            [ReadOnly] public float CurTime;
            [ReadOnly] public ComponentLookup<ExpData> ExpData;
            [NativeDisableParallelForRestriction] public BufferLookup<LightShieldDefenderData> DefenderLookup;
            [ReadOnly] public ComponentLookup<BasicStateData> StateData;

            private void Execute(ref DynamicBuffer<AoeTarget> targets,
                ref LightShieldBuffData data, ref DynamicBuffer<PreviousAoeTarget> previousAoeTargets)
            {
                if (!StateData.TryGetComponent(data.Defender, out var stateData) ||
                    stateData.CurState != InteractState.Attacking)
                {
                    if (previousAoeTargets.Length != 0)
                    {
                        RemoveOutOfRange(ref targets, data, ref previousAoeTargets, true);
                    }

                    return; // General buff system will remove this buff
                }

                if (CurTime < data.TriggerTime) return;

                data.TriggerTime = CurTime + data.CheckDuration;
                var ifTier3 = ExpData[data.Defender].CurTier == Tier.Tier3;

                // Remove old, out of range defender
                RemoveOutOfRange(ref targets, data, ref previousAoeTargets, false);
                // Add new defender
                AddNewShieldData(ref targets, data, ref previousAoeTargets, ifTier3);
            }

            private void AddNewShieldData(ref DynamicBuffer<AoeTarget> targets, in LightShieldBuffData data,
                ref DynamicBuffer<PreviousAoeTarget> previousAoeTargets,
                bool ifTier3)
            {
                for (var i = targets.Length - 1; i >= 0; i--)
                {
                    var target = targets[i];
                    if (!DefenderLookup.TryGetBuffer(target.Entity, out var buffer))
                    {
                        continue; // Cur ally is dead
                    }

                    var find = false;
                    foreach (var defender in buffer)
                    {
                        if (defender.Entity == data.Defender)
                        {
                            find = true;
                            break;
                        }
                    }

                    if (!find)
                    {
                        buffer.Add(new LightShieldDefenderData
                        {
                            Entity = data.Defender,
                            SelfGetDamageScale = data.SelfGetDamageScale,
                            GetDamageScale = data.GetDamageScale,
                            IfTier3 = ifTier3
                        });
                    }
                }

                previousAoeTargets.Clear();
                foreach (var target in targets)
                {
                    previousAoeTargets.Add(new PreviousAoeTarget
                    {
                        Entity = target.Entity
                    });
                }
            }

            private void RemoveOutOfRange(ref DynamicBuffer<AoeTarget> targets, in LightShieldBuffData data,
                ref DynamicBuffer<PreviousAoeTarget> previousAoeTargets, bool ifRemoveAll)
            {
                for (var i = previousAoeTargets.Length - 1; i >= 0; i--)
                {
                    var previousTarget = previousAoeTargets[i];
                    if (!DefenderLookup.TryGetBuffer(previousTarget.Entity, out var previousDefenderData))
                        continue;
                    var outOfRange = true;
                    if (!ifRemoveAll)
                    {
                        foreach (var curTarget in targets)
                        {
                            if (curTarget.Entity == previousTarget.Entity)
                            {
                                outOfRange = false;
                                break;
                            }
                        }
                    }
                    if (!outOfRange) continue;
                    for (int j = previousDefenderData.Length - 1; j >= 0; j--)
                    {
                        var defender = previousDefenderData[j];
                        // This defender is dead or out of range, should be removed
                        if (defender.Entity == data.Defender)
                        {
                            previousDefenderData.RemoveAt(j);
                        }
                    }
                }
            }
        }
    }
}
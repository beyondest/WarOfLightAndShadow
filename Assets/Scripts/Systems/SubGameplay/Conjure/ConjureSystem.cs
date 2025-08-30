using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;

namespace SparFlame.Systems.SubGameplay.Conjure
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ConjureSystem : ISystem
    {
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<AIConjureShrineData> _enemyConjuringDataLookUp;
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private NativeHashSet<Entity> _alreadyTagged;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<ConjureSystemConfig>();
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _enemyConjuringDataLookUp = state.GetComponentLookup<AIConjureShrineData>();
            _generalAttrLookup = state.GetComponentLookup<SubGameplayGeneralAttr>();
            _alreadyTagged = new NativeHashSet<Entity>(16, Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_alreadyTagged.IsCreated)
                _alreadyTagged.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            // var ecbP = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            // var config = SystemAPI.GetSingleton<ConjureSystemConfig>();

            CheckConjureUnitsRequest(ref state, ecb);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            var ecbp = new EntityCommandBuffer(Allocator.TempJob);
            _enemyConjuringDataLookUp.Update(ref state);
            _unitAttrLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            var job = new ConjureJob
            {
                ECB = ecbp.AsParallelWriter(),
                DeltaHour = SystemAPI.GetSingleton<WorldTimeData>().deltaHour,
                UnitAttrLookup = _unitAttrLookup,
                EnemyConjuringLookUp = _enemyConjuringDataLookUp,
                GeneralAttrLookup = _generalAttrLookup
            }.ScheduleParallel(state.Dependency);
            job.Complete();
            ecbp.Playback(state.EntityManager);
            ecbp.Dispose();
        }

        private void CheckConjureUnitsRequest(ref SystemState state, EntityCommandBuffer ecb)
        {
            _alreadyTagged.Clear();
            foreach (var (ro, entity) in SystemAPI.Query<RefRO<ConjureRequest>>().WithEntityAccess())
            {
                var request = ro.ValueRO;
                // Safety check
                if (!SystemAPI.HasBuffer<ConjuringData>(request.BuildingEntity))
                {
                    ecb.DestroyEntity(entity);
                    continue;
                }

                var buffer = SystemAPI.GetBuffer<ConjuringData>(request.BuildingEntity);
                if (_alreadyTagged.Add(request.BuildingEntity))
                    ecb.AddComponent<ConjuringTag>(request.BuildingEntity);
                // Add task to building buffer
                var timeCost = request.Count * SystemAPI.GetComponent<UnitAttr>(request.UnitPrefab)
                    .ConjureSpeedHoursPerUnit;
                int i;
                for (i = 0; i < buffer.Length; i++)
                {
                    var data = buffer[i];
                    if (data.ConjuringEntity == request.UnitPrefab)
                    {
                        break;
                    }
                }

                if (i == buffer.Length)
                {
                    buffer.Add(new ConjuringData
                    {
                        ConjuringEntity = request.UnitPrefab,
                        TargetAmount = request.Count,
                        ConjuredAmount = 0,
                        AccumulatedHours = 0,
                        RemainingTimeHours = timeCost
                    });
                }
                else
                {
                    var data = buffer[i];
                    data.TargetAmount += request.Count;
                    data.RemainingTimeHours += timeCost;
                    buffer[i] = data;
                }

                ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        [WithNone(typeof(OocTag))]
        [WithAll(typeof(ConjuringTag))]
        private partial struct ConjureJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float DeltaHour;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<AIConjureShrineData> EnemyConjuringLookUp;

            private void Execute([ChunkIndexInQuery] int index, in ConjureAttr conjureAttr,
                ref DynamicBuffer<ConjuringData> conjuringData,
                in LocalTransform transform,
                Entity selfEntity)
            {
                var selfGeneralAttr = GeneralAttrLookup[selfEntity];
                if (conjuringData.Length == 0)
                {
                    ECB.RemoveComponent<ConjuringTag>(index, selfEntity);
                    return;
                }

                var data = conjuringData[0];

                data.AccumulatedHours += DeltaHour;
                data.RemainingTimeHours -= DeltaHour;
                data.RemainingTimeHours = math.max(0, data.RemainingTimeHours);

                var hoursPerUnit = UnitAttrLookup[data.ConjuringEntity].ConjureSpeedHoursPerUnit;
                if (data.AccumulatedHours >= hoursPerUnit)
                {
                    var count = (int)(data.AccumulatedHours / hoursPerUnit);
                    data.AccumulatedHours %= hoursPerUnit;
                    count = math.min(count, data.TargetAmount - data.ConjuredAmount);
                    data.ConjuredAmount += count;

                    for (var i = 0; i < count; i++)
                    {
                        var unit = ECB.Instantiate(index, data.ConjuringEntity);
                        ECB.AddComponent<SubGameplayEntityTag>(index, unit);
                        var generalAttr = GeneralAttrLookup[data.ConjuringEntity];
                        generalAttr.SubFaction = selfGeneralAttr.SubFaction;
                        ECB.SetComponent(index, unit, generalAttr);

                        var transformCopy = transform;
                        var pos = transformCopy.TransformPoint(conjureAttr.ConjurePositionBias);
                        // var pos = transform.Position + conjureAttr.ConjurePositionBias;
                        ECB.SetComponent(index, unit, new LocalTransform
                        {
                            Position = pos,
                            Rotation = quaternion.identity,
                            Scale = 1f
                        });

                        // Add Enemy Base Data for AI system
                        if (EnemyConjuringLookUp.TryGetComponent(selfEntity, out var enemyConjureShrineData))
                        {
                            ECB.AddComponent(index, unit, new AIUnitBelongsTo
                            {
                                Base = enemyConjureShrineData.Base
                            });
                        }

                        var vfxRequest = ECB.CreateEntity(index);
                        ECB.AddComponent<SubGameplayEntityTag>(index, vfxRequest);
                        ECB.AddComponent(index, vfxRequest, new VFXRequest
                        {
                            ParabolaTargetPosition = default,
                            Filter = new VFXSubFilter
                            {
                                Faction = selfGeneralAttr.Faction,
                                FactionFilterEnable = true,
                                Tier = default,
                                TierFilterEnable = false
                            },
                            KeepDuration = 0,
                            SpawnPosition = pos,
                            StatChangeRequest = default,
                            VFXName = VFXName.ConjureUnit,
                            VFXTrackTarget = Entity.Null,
                            RequestType = VFXRequestType.Spawn
                        });
                    }
                }
                if (data.ConjuredAmount >= data.TargetAmount)
                {
                    conjuringData.RemoveAt(0);
                }
                else
                {
                    conjuringData[0] = data;
                }
            }
        }
    }
}
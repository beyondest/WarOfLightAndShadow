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
        private ComponentLookup<SubGameplayGeneralAttr> _generalAttrLookup;
        private NativeHashSet<Entity> _alreadyTagged;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SubGameStatusData>();
            state.RequireForUpdate<WorldTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<ConjureSystemConfig>();
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
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
            var debug = new ConjureDebug();
            if (SystemAPI.HasSingleton<DebugTag>())
                SystemAPI.TryGetSingleton(out debug);
            CheckConjureUnitsRequest(ref state, ecb, debug);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            var ecbp = new EntityCommandBuffer(Allocator.TempJob);
            _unitAttrLookup.Update(ref state);
            _generalAttrLookup.Update(ref state);
            var job = new ConjureJob
            {
                ECB = ecbp.AsParallelWriter(),
                CurTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours,
                UnitAttrLookup = _unitAttrLookup,
                GeneralAttrLookup = _generalAttrLookup,
                ConjureDebug = debug
            }.ScheduleParallel(state.Dependency);
            job.Complete();
            ecbp.Playback(state.EntityManager);
            ecbp.Dispose();
        }

        private void CheckConjureUnitsRequest(ref SystemState state, EntityCommandBuffer ecb, in ConjureDebug debug)
        {
            _alreadyTagged.Clear();
            var curTotalHours = SystemAPI.GetSingleton<WorldTimeData>().totalHours;
            var city = SystemAPI.GetSingleton<SubGameStatusData>().City;
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
                var hoursPerUnit = SystemAPI.GetComponent<UnitAttr>(request.UnitPrefab).conjureSpeedHoursPerUnit;
                if (debug is { enabled: true, conjureHoursScale: > 0 })
                    hoursPerUnit *= debug.conjureHoursScale;
                var uniqueId = SystemAPI.GetComponent<GlobalSingleId>(request.BuildingEntity).value;

                // Add task to city buffer
                var resourceChangeRequest = ecb.CreateEntity();
                ecb.AddComponent(resourceChangeRequest, new ResourceChangeRequest
                {
                    City = city,
                    ResourceType = ResourceType.SoulPact,
                    RequestType = ResourceRequestType.ConjureUnitByTask,
                    AbsAmount = request.Count,
                    HoursPerUnit = hoursPerUnit,
                    FromBuildingSingleId = uniqueId,
                });
                ecb.AddComponent<SubGameplayEntityTag>(resourceChangeRequest);

                // Add task to building buffer
                var timeCost = request.Count * hoursPerUnit;
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
                        PrefabId = SystemAPI.GetComponent<PrefabId>(request.UnitPrefab).value,
                        TargetAmount = request.Count,
                        ConjuredAmount = 0,
                        LastCheckTotalHours = curTotalHours,
                        ThisTaskRemainingTime = timeCost
                    });
                }
                else
                {
                    var data = buffer[i];
                    data.TargetAmount += request.Count;
                    data.ThisTaskRemainingTime += timeCost;
                    buffer[i] = data;
                }

                ecb.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        [WithAll(typeof(ConjuringTag))]
        private partial struct ConjureJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public float CurTotalHours;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
            [ReadOnly] public ComponentLookup<SubGameplayGeneralAttr> GeneralAttrLookup;
            [ReadOnly] public ConjureDebug ConjureDebug;

            private void Execute([ChunkIndexInQuery] int index, in ConjureAttr conjureAttr,
                ref DynamicBuffer<ConjuringData> conjuringDatas,
                in LocalTransform transform,
                Entity selfEntity)
            {
                var selfGeneralAttr = GeneralAttrLookup[selfEntity];
                if (conjuringDatas.Length == 0)
                {
                    ECB.RemoveComponent<ConjuringTag>(index, selfEntity);
                    return;
                }

                var deltaHours = CurTotalHours - conjuringDatas[0].LastCheckTotalHours;

                while (conjuringDatas.Length != 0)
                {
                    var firstData = conjuringDatas[0];
                    var hoursPerUnit = UnitAttrLookup[firstData.ConjuringEntity].conjureSpeedHoursPerUnit;
                    if (ConjureDebug is { enabled: true, conjureHoursScale: > 0 })
                        hoursPerUnit *= ConjureDebug.conjureHoursScale;
                    var maxCount = firstData.TargetAmount - firstData.ConjuredAmount;
                    firstData.ThisTaskRemainingTime = math.max(0, maxCount * hoursPerUnit - deltaHours);

                    if (deltaHours < hoursPerUnit)
                    {
                        conjuringDatas[0] = firstData;
                        break;
                    }

                    var count = (int)(deltaHours / hoursPerUnit);
                    var validCount = math.min(count, maxCount);
                    var validHours = validCount * hoursPerUnit;
                    firstData.LastCheckTotalHours += validHours;
                    deltaHours -= validHours;

                    firstData.ConjuredAmount += validCount;

                    // Spawn units
                    for (var i = 0; i < validCount; i++)
                    {
                        var unit = ECB.Instantiate(index, firstData.ConjuringEntity);
                        ECB.AddComponent<SubGameplayEntityTag>(index, unit);

                        var generalAttr = GeneralAttrLookup[firstData.ConjuringEntity];
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

                    // Pass conjuring queue
                    if (firstData.ConjuredAmount >= firstData.TargetAmount)
                    {
                        conjuringDatas.RemoveAt(0);
                        if (conjuringDatas.Length > 0)
                        {
                            var newConjuringData = conjuringDatas[0];
                            newConjuringData.LastCheckTotalHours = firstData.LastCheckTotalHours;
                            conjuringDatas[0] = newConjuringData;
                        }
                    }
                    else
                    {
                        conjuringDatas[0] = firstData;
                    }
                }
            }
        }
    }
}
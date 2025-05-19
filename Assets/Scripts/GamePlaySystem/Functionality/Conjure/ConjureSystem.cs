using SparFlame.GamePlaySystem.CustomParticleSystem;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Units;
using Unity.Burst;
using Unity.Collections;

namespace SparFlame.GamePlaySystem.Conjure
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct ConjureSystem : ISystem
    {
        private ComponentLookup<UnitAttr> _unitAttrLookup;
        private ComponentLookup<EnemyConjureShrineData> _enemyConjuringDataLookUp;
        private NativeHashSet<Entity> _alreadyTagged;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GamingTag>();
            state.RequireForUpdate<ConjureSystemConfig>();
            _unitAttrLookup = state.GetComponentLookup<UnitAttr>(true);
            _enemyConjuringDataLookUp = state.GetComponentLookup<EnemyConjureShrineData>();
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
            var job =new ConjureJob
            {
                ECB = ecbp.AsParallelWriter(),
                DeltaTime = SystemAPI.GetSingleton<GameTimeData>().DeltaTime,
                UnitAttrLookup = _unitAttrLookup,
                EnemyConjuringLookUp = _enemyConjuringDataLookUp
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
                    .ConjureSpeedSecondPerUnit;
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
                        Counter = 0,
                        RemainingTimeSeconds = timeCost
                    });
                }
                else
                {
                    var data = buffer[i];
                    data.TargetAmount += request.Count;
                    data.RemainingTimeSeconds += timeCost;
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
            public float DeltaTime;
            [ReadOnly] public ComponentLookup<UnitAttr> UnitAttrLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<EnemyConjureShrineData> EnemyConjuringLookUp;

            private void Execute([ChunkIndexInQuery] int index, in ConjureAttr conjureAttr,
                ref DynamicBuffer<ConjuringData> conjuringData,
                in LocalTransform transform, in GeneralAttr generalAttr,
                Entity entity)
            {
                if (conjuringData.Length == 0)
                {
                    ECB.RemoveComponent<ConjuringTag>(index, entity);
                    return;
                }

                var data = conjuringData[0];
                var speed = 1 / UnitAttrLookup[data.ConjuringEntity].ConjureSpeedSecondPerUnit;
                data.Counter += DeltaTime * speed;
                data.RemainingTimeSeconds -= DeltaTime;
                conjuringData[0] = data;
                // Conjure unit when time arrived
                if (data.Counter >= 1)
                {
                    var unit = ECB.Instantiate(index, data.ConjuringEntity);
                    
                    ECB.AddComponent<GameplayEntityTag>(index, unit);
                    var transformCopy = transform;
                    var pos = transformCopy.TransformPoint(conjureAttr.ConjurePositionBias);
                    // var pos = transform.Position + conjureAttr.ConjurePositionBias;
                    ECB.SetComponent(index, unit, new LocalTransform
                    {
                        Position = pos, // TODO : Make sure unit can never be stuck when it is conjured
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    data.Counter = 0;
                    data.ConjuredAmount++;
                    if (data.ConjuredAmount >= data.TargetAmount)
                    {
                        conjuringData.RemoveAt(0);
                    }
                    else
                    {
                        conjuringData[0] = data;
                    }
                    
                    // Add Enemy Base Data for AI system
                    if (EnemyConjuringLookUp.TryGetComponent(entity, out var enemyConjureShrineData))
                    {
                        ECB.AddComponent(index,unit,new EnemyUnitBelongsTo
                        {
                            Base = enemyConjureShrineData.Base
                        });
                    }

                    var vfxRequest = ECB.CreateEntity(index);
                    ECB.AddComponent<GameplayEntityTag>(index, vfxRequest);
                    ECB.AddComponent(index, vfxRequest, new VFXRequest
                    {
                        TargetPosition = default,
                        Filter = new VFXSubFilter
                        {
                            Faction = generalAttr.FactionTag,
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
        }
    }
}
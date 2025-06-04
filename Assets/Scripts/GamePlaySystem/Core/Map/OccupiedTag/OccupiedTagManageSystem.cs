using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using EntityCommandBuffer = Unity.Entities.EntityCommandBuffer;

namespace SparFlame.GamePlaySystem.Resource
{
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct OccupiedTagManageSystem : ISystem
    {
        private ComponentLookup<CrystalPosVector4Override> _crystalPosRecordLookUp;
        private ComponentLookup<CrystalPos2Vector4Override> _crystalPosRecordLookUp2;
        private ComponentLookup<CrystalPos3Vector4Override> _crystalPosRecordLookUp3;
        private ComponentLookup<CrystalPos4Vector4Override> _crystalPosRecordLookUp4;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CrystalAffectMapRadiusSq>();
            state.RequireForUpdate<ChangeOccupiedTagRequest>();
            state.RequireForUpdate<GamingTag>();
            _crystalPosRecordLookUp = state.GetComponentLookup<CrystalPosVector4Override>();
            _crystalPosRecordLookUp2 = state.GetComponentLookup<CrystalPos2Vector4Override>();
            _crystalPosRecordLookUp3 = state.GetComponentLookup<CrystalPos3Vector4Override>();
            _crystalPosRecordLookUp4 = state.GetComponentLookup<CrystalPos4Vector4Override>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _crystalPosRecordLookUp4.Update(ref state);
            _crystalPosRecordLookUp3.Update(ref state);
            _crystalPosRecordLookUp2.Update(ref state);
            _crystalPosRecordLookUp.Update(ref state);
            var config = SystemAPI.GetSingleton<CrystalAffectMapRadiusSq>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ChangeOccupiedTagRequest>>().WithEntityAccess())
            {
                var crystalPos = request.ValueRO.CrystalPos;
                if (request.ValueRO.IsDestroyed)
                {
                    ecb.DestroyEntity(entity);
                }
                else
                {
                    if(!SystemAPI.HasComponent<LocalTransform>(entity))
                        continue;
                    ecb.RemoveComponent<ChangeOccupiedTagRequest>(entity);
                    crystalPos = SystemAPI.GetComponent<LocalTransform>(entity).Position;
                }
                var job = new ChangeOccupiedTagJob
                {
                    Config = config,
                    ChangeIntoFaction =
                        request.ValueRO.IsDestroyed ? FactionTag.Neutral : request.ValueRO.CrystalFaction,
                    CrystalPos = crystalPos,
                    ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                    CrystalPosRecord = _crystalPosRecordLookUp,
                    CrystalPosRecord2 = _crystalPosRecordLookUp2,
                    CrystalPosRecord3 = _crystalPosRecordLookUp3,
                    CrystalPosRecord4 = _crystalPosRecordLookUp4,
                }.ScheduleParallel(state.Dependency);
                job.Complete();
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct ChangeOccupiedTagJob : IJobEntity
        {
            [ReadOnly] public FactionTag ChangeIntoFaction;
            [ReadOnly] public float3 CrystalPos;
            [ReadOnly] public CrystalAffectMapRadiusSq Config;
            [ReadOnly] public MapInfo Info;
            [ReadOnly] public float TileSize;
            [NativeDisableParallelForRestriction] public ComponentLookup<CrystalPosVector4Override> CrystalPosRecord;
            [NativeDisableParallelForRestriction] public ComponentLookup<CrystalPos2Vector4Override> CrystalPosRecord2;
            [NativeDisableParallelForRestriction] public ComponentLookup<CrystalPos3Vector4Override> CrystalPosRecord3;
            [NativeDisableParallelForRestriction] public ComponentLookup<CrystalPos4Vector4Override> CrystalPosRecord4;


            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<LinkedEntityGroup> group,
                ref OccupiedTag tag,
                in LocalTransform transform, ref CrystalRecordData data, ref DynamicBuffer<EnvEntities> envEntities)
            {
                // By limiting the player crystal build position and enemy crystal build position, dark and light crystal impact on same tile will never happen
                if (!MapUtils.IsTileInCrystalRadius(transform.Position, CrystalPos, Config.Value,
                        TileSize
                    )) return;

                var meshChild = group[1].Value;
                var shouldResetToNeg = ChangeIntoFaction == FactionTag.Neutral;
                ECB.SetComponent(index, meshChild, new CrystalRadiusFloatOverride
                {
                    Value = math.sqrt(Config.Value)
                });
                
                ref var pos1 = ref CrystalPosRecord.GetRefRW(meshChild).ValueRW;
                ref var pos2 = ref CrystalPosRecord2.GetRefRW(meshChild).ValueRW;
                ref var pos3 = ref CrystalPosRecord3.GetRefRW(meshChild).ValueRW;
                ref var pos4 = ref CrystalPosRecord4.GetRefRW(meshChild).ValueRW;

                if (shouldResetToNeg)
                {
                    if (math.distancesq(CrystalPos, pos1.Value.xyz) <= 0.5f)
                    {
                        pos1.Value.y = -1f;
                    }
                    else if (math.distancesq(CrystalPos, pos2.Value.xyz) <= 0.5f)
                    {
                        pos2.Value.y = -1f;
                    }
                    else if (math.distancesq(CrystalPos, pos3.Value.xyz) <= 0.5f)
                    {
                        pos3.Value.y = -1f;
                    }
                    else if (math.distancesq(CrystalPos, pos4.Value.xyz) <= 0.5f)
                    {
                        pos4.Value.y = -1f;
                    }
                    else
                    {
                        // When this destroyed crystal is not recorded, it should not affect this tile 
                        data.RecordCount++;
                    }

                    data.RecordCount--;
                    if (data.RecordCount == 0)
                        tag.Faction = FactionTag.Neutral;
                    return;
                }

                var recordPos = new float4(CrystalPos.x, CrystalPos.y, CrystalPos.z, 0f);
                if (pos1.Value.y < 0f)
                {
                    pos1.Value = recordPos;
                }
                else if (pos2.Value.y < 0f)
                {
                    pos2.Value = recordPos;
                }
                else if (pos3.Value.y < 0f)
                {
                    pos3.Value = recordPos;
                }
                else if (pos4.Value.y < 0f)
                {
                    pos4.Value = recordPos;
                }
                else
                {
                    // When no record slot left, this built crystal do not affect the tile 
                    data.RecordCount--;
                }

                data.RecordCount++;
                if (data.RecordCount == 1)
                {
                    tag.Faction = ChangeIntoFaction;
                    foreach (var envEntity in envEntities)
                    {
                        ECB.DestroyEntity(index, envEntity.Value);
                    }

                    if (tag.Faction == FactionTag.Ally)
                    {
                        ECB.SetComponent(index, meshChild, new IsLightFloatOverride
                        {
                            Value = 1
                        });
                    }

                    if (tag.Faction == FactionTag.Enemy)
                    {
                        ECB.SetComponent(index, meshChild, new IsLightFloatOverride
                        {
                            Value = 0
                        });
                    }
                    
                    envEntities.Clear();
                    
                }
            }
        }
    }
}
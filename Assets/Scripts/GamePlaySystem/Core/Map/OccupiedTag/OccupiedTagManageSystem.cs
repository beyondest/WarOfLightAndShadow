using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
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
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<OccupiedManageSystemConfig>();
            state.RequireForUpdate<ChangeOccupiedTagRequest>();
            state.RequireForUpdate<GamingTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<OccupiedManageSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ChangeOccupiedTagRequest>>().WithEntityAccess())
            {
                var job = new ChangeOccupiedTagJob
                {
                    Config = config,
                    ChangeIntoFaction =
                        request.ValueRO.IsDestroyed ? FactionTag.Neutral : request.ValueRO.CrystalFaction,
                    CrystalPos = request.ValueRO.CrystalPos,
                    ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
                }.ScheduleParallel(state.Dependency);
                job.Complete();
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct ChangeOccupiedTagJob : IJobEntity
        {
            [ReadOnly] public FactionTag ChangeIntoFaction;
            [ReadOnly] public float3 CrystalPos;
            [ReadOnly] public OccupiedManageSystemConfig Config;
            [ReadOnly] public MapInfo Info;
            [ReadOnly] public float TileSize;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<LinkedEntityGroup> group,
                ref OccupiedTag tag,
                in LocalTransform transform, ref CrystalPosVector4Override pos1,ref CrystalPos2Vector4Override pos2,
                ref CrystalPos3Vector4Override pos3, ref CrystalPos4Vector4Override pos4,ref CrystalRecordData data)
            {
                // By limiting the player crystal build position and enemy crystal build position, dark and light crystal impact on same tile will never happen
                if (!MapUtils.IsTileInCrystalRadius(transform.Position, CrystalPos, Config.CrystalChangeRadiusSq,
                        TileSize
                    )) return;

                var meshChild = group[1].Value;
                var isLight = ChangeIntoFaction == FactionTag.Ally ? 1f : 0f;
                var shouldResetToNeg = ChangeIntoFaction == FactionTag.Neutral;
                ECB.SetComponent(index, meshChild, new CrystalRadiusFloatOverride
                {
                    Value = Config.CrystalChangeRadiusSq
                });
                ECB.SetComponent(index, meshChild, new IsLightFloatOverride
                {
                    Value = isLight
                });
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
                    if(data.RecordCount == 0)
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
                    tag.Faction = ChangeIntoFaction;
            }
        }
    }
}
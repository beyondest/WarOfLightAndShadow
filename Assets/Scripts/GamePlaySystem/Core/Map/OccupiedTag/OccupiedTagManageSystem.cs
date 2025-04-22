using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using EntityCommandBuffer = Unity.Entities.EntityCommandBuffer;

namespace SparFlame.GamePlaySystem.Resource
{
    public partial struct OccupiedTagManageSystem : ISystem
    {
        private ComponentLookup<MaterialMeshInfo> _materialLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OccupiedManageSystemConfig>();
            state.RequireForUpdate<ChangeOccupiedTagRequest>();
            _materialLookup = state.GetComponentLookup<MaterialMeshInfo>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<OccupiedManageSystemConfig>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            _materialLookup.Update(ref state);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<ChangeOccupiedTagRequest>>().WithEntityAccess())
            {
                Debug.Log($"{request.ValueRO.CrystalFaction},{request.ValueRO.DestroyedCrystalPos}");
                new ChangeOccupiedTagJob
                {
                    MaterialLookup = _materialLookup,
                    Config = config,
                    ChangeIntoFaction =
                        request.ValueRO.IsDestroyed ? FactionTag.Neutral : request.ValueRO.CrystalFaction,
                    Position = request.ValueRO.DestroyedCrystalPos
                }.ScheduleParallel();
                ecb.DestroyEntity(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }


        [BurstCompile]
        private partial struct ChangeOccupiedTagJob : IJobEntity
        {
            [ReadOnly] public FactionTag ChangeIntoFaction;
            [ReadOnly] public float3 Position;
            [ReadOnly] public OccupiedManageSystemConfig Config;
            [NativeDisableParallelForRestriction] public ComponentLookup<MaterialMeshInfo> MaterialLookup;

            private void Execute(ref DynamicBuffer<LinkedEntityGroup> group, ref OccupiedTag tag,
                in LocalTransform transform)
            {
                if(!MapUtils.IsInsideGrid(transform.Position,Config.TileSize,Position))return;
                tag.Faction = ChangeIntoFaction;
                var meshChild = group[1].Value;
                PlayChangeOccupiedTagAnimation(tag.Faction, ref MaterialLookup.GetRefRW(meshChild).ValueRW);
            }

            private void PlayChangeOccupiedTagAnimation(FactionTag faction, ref MaterialMeshInfo materialInfo)
            {
                Debug.Log($"{materialInfo.Material}{faction}");
                materialInfo.Material = faction switch
                {
                    FactionTag.Neutral => MaterialLookup[Config.NeutralOccupiedRef].Material,
                    FactionTag.Ally => MaterialLookup[Config.AllyOccupiedRef].Material,
                    FactionTag.Enemy => MaterialLookup[Config.EnemyOccupiedRef].Material,
                    _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null)
                };
                Debug.Log($"{materialInfo.Material}");
            }
        }
    }
}
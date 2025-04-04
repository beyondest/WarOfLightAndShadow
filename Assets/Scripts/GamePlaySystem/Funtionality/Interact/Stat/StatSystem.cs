﻿using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.PopNumber;
using Unity.Transforms;

namespace SparFlame.GamePlaySystem.Interact
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct StatSystem : ISystem
    {
        private ComponentLookup<InteractableAttr> _interactableAttrLookup;
        private ComponentLookup<VolumeObstacleTag> _volumeObstacleTagLookup;
        private ComponentLookup<StatData> _statDataLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private BufferLookup<InsightTarget> _insightTargetLookup;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<StatSystemConfig>();
            state.RequireForUpdate<SightSystemConfig>();
            _interactableAttrLookup = state.GetComponentLookup<InteractableAttr>(true);
            _volumeObstacleTagLookup = state.GetComponentLookup<VolumeObstacleTag>(true);
            _statDataLookup = state.GetComponentLookup<StatData>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(true);
            _insightTargetLookup = state.GetBufferLookup<InsightTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _statDataLookup.Update(ref state);
            _interactableAttrLookup.Update(ref state);
            _volumeObstacleTagLookup.Update(ref state);
            _insightTargetLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            var autoChooseTargetSystemConfig = SystemAPI.GetSingleton<SightSystemConfig>();
            var config = SystemAPI.GetSingleton<StatSystemConfig>();
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            new CheckStatChangeRequest
            {
                ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                InteractableAttrLookup = _interactableAttrLookup,
                ObstacleTagLookup = _volumeObstacleTagLookup,
                StatLookup = _statDataLookup,
                TransformLookup = _localTransformLookup,
                TargetListLookup = _insightTargetLookup,
                SightConfig = autoChooseTargetSystemConfig,
                Config = config
            }.ScheduleParallel();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }


        [BurstCompile]
        public partial struct CheckStatChangeRequest : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            
            [NativeDisableParallelForRestriction] public BufferLookup<InsightTarget> TargetListLookup;
            [NativeDisableParallelForRestriction] public ComponentLookup<StatData> StatLookup;
            [ReadOnly] public ComponentLookup<InteractableAttr> InteractableAttrLookup;
            [ReadOnly] public ComponentLookup<VolumeObstacleTag> ObstacleTagLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public SightSystemConfig SightConfig;
            [ReadOnly] public StatSystemConfig Config;
            

            private void Execute([ChunkIndexInQuery] int index, in StatChangeRequest request, Entity entity)
            {
                // TODO : Check where wrong
                // These 2 should not happen when the stat system updates after interact state machine, but it happens sometimes.
                if(!StatLookup.HasComponent(request.Interactee))return;
                if(!InteractableAttrLookup.TryGetComponent(request.Interactor,out var interactableAttr))return;
                
                // Handle Stat Change Request. This request is destroyed other place, like pop number system
                ref var statInteractee = ref StatLookup.GetRefRW(request.Interactee).ValueRW;
                
                // This entity is already dead and handled by other request handling process
                if(statInteractee.CurValue <=0)return;
                
                statInteractee.CurValue = request.InteractType == InteractType.Heal
                    ? math.min(statInteractee.MaxValue, statInteractee.CurValue + request.Amount)
                    : math.max(0, statInteractee.CurValue - request.Amount);
                
                var popNumberType = PopNumberType.DamageDealt;
                var interactorFaction = interactableAttr.FactionTag;
                popNumberType = (request.InteractType, interactorFaction) switch
                {
                    (InteractType.Heal, FactionTag.Ally) => PopNumberType.AllyHealed,
                    (InteractType.Attack, FactionTag.Ally) => PopNumberType.DamageDealt,
                    (InteractType.Heal, FactionTag.Enemy) => PopNumberType.EnemyHealed,
                    (InteractType.Attack, FactionTag.Enemy) => PopNumberType.DamageTaken,
                    (InteractType.Harvest, FactionTag.Ally) => PopNumberType.AllyHarvest,
                    (InteractType.Harvest, FactionTag.Enemy) => PopNumberType.EnemyHarvest,
                    _ => popNumberType
                };
                var interacteePos = TransformLookup[request.Interactee].Position;
                
                // Spawn Pop Number VFX
                var popNumberRequest = ECB.CreateEntity(index);
                ECB.AddComponent(index,popNumberRequest, new PopNumberRequest
                {
                    ColorId = (int)popNumberType,
                    Position = interacteePos,
                    Scale = Config.PopNumberScale,
                    Value = request.Amount
                });
                
                // Raise attacker statChangeValue in interactee target list 
                if (request.InteractType == InteractType.Attack && statInteractee.CurValue > 0)
                {
                    // Target may be no interact ability target, like walls, they don't have targetsBuffer
                    if (TargetListLookup.TryGetBuffer(request.Interactee, out var targetsBuffer))
                    {
                        for (var i = 0; i < targetsBuffer.Length; i++)
                        {
                            var target = targetsBuffer[i];
                            if (target.Entity == request.Interactor)
                            {
                                var statChangeValue = CalStatChangeValue(request.Amount, ref SightConfig);
                                target.StatChangValue += statChangeValue;
                                targetsBuffer[i] = target;
                            }
                        }
                    }
                }

                // Remove Dead Entities, like units, resources, buildings
                if (statInteractee.CurValue <= 0)
                {
                    RemoveAndSendRequest(request.Interactee, index);
                }
                ECB.DestroyEntity(index, entity);
            }

            /// <summary>
            /// This method should contain every post process of that dead entity,
            /// cause entity is destroyed and invalid after this system update
            /// </summary>
            /// <param name="entity"></param>
            /// <param name="index"></param>
            private void RemoveAndSendRequest(Entity entity, int index)
            {
                var interactableAttr = InteractableAttrLookup[entity];
                SendUpdateResourcesRequest(interactableAttr,entity,index);
                switch (interactableAttr.BaseTag)
                {
                    case BaseTag.Units:
                        break;
                    case BaseTag.Buildings:
                        if (ObstacleTagLookup.HasComponent(entity))
                        {
                            var destroyObstacleRequest = ECB.CreateEntity(index);
                            ECB.AddComponent(index,destroyObstacleRequest, new VolumeObstacleDestroyRequest
                            {
                                FromEntity = entity,
                                RequestFromFaction = interactableAttr.FactionTag
                            });
                        }
                        break;
                    case BaseTag.Resources:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                ECB.DestroyEntity(index,entity);
            }

            private void SendUpdateResourcesRequest(InteractableAttr interactableAttr, Entity entity, int index)
            {
                // Debug.Log($"Dead : BaseTag {interactableAttr.BaseTag}, factionTag {interactableAttr.FactionTag}");
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static float CalStatChangeValue(float requestAmount, ref SightSystemConfig config)
            {
                return requestAmount * config.StatValueChangeMultiplier;
            }
        }
    }
}
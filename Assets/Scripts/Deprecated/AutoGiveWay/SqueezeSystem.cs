// using Unity.Entities;
// using Unity.Burst;
// using Unity.Transforms;
// using Unity.Mathematics;
// using Unity.Collections;
// using Unity.Physics;
// using SparFlame.GamePlaySystem.General;
// using Unity.Android.Gradle;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     [BurstCompile]
//     public partial struct SqueezeSystem : ISystem
//     {
//         private ComponentLookup<InteractableAttr> _interactAttrLookup;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<MovementConfig>();
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//             state.RequireForUpdate<NotPauseTag>();
//             state.RequireForUpdate<AutoGiveWaySystemConfig>();
//             state.RequireForUpdate<SqueezeSystemConfig>();
//             _interactAttrLookup = state.GetComponentLookup<InteractableAttr>(true);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//             _interactAttrLookup.Update(ref state);
//             var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//             var config = SystemAPI.GetSingleton<AutoGiveWaySystemConfig>();
//             var movementConfig = SystemAPI.GetSingleton<MovementConfig>();
//
//             var squeezeJobHandle =new SqueezeJob
//             {
//                 PhysicsWorld = physicsWorld,
//                 InteractAttrLookup = _interactAttrLookup,
//                 ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                 ObstacleLayerMask = movementConfig.ObstacleLayerMask,
//                 DetectRayBelongsTo = movementConfig.DetectRaycasstBelongsTo,
//                 SqueezeColliderDetectionRatio = config.SqueezeColliderDetectionRatio
//             }.ScheduleParallel(state.Dependency);
//             state.Dependency = squeezeJobHandle;
//         }
//
//
//         [BurstCompile]
//         public partial struct SqueezeJob : IJobEntity
//         {
//             [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
//             [ReadOnly] public ComponentLookup<InteractableAttr> InteractAttrLookup;
//             public EntityCommandBuffer.ParallelWriter ECB;
//             [ReadOnly] public uint ObstacleLayerMask;
//             [ReadOnly] public uint DetectRayBelongsTo;
//             [ReadOnly] public float SqueezeColliderDetectionRatio;
//
//             private void Execute([ChunkIndexInQuery] int index, ref LocalTransform transform, in SqueezeData data,
//                 in MovableData movableData,
//                 Entity entity
//             )
//             {
//                 var direction = math.normalize(data.MoveVector);
//                 var moveDelta = math.length(data.MoveVector);
//
//                 // Try pass squeeze data 
//                 if (MovementUtils.ObstacleInDirection(ref PhysicsWorld,
//                         math.max(movableData.SelfColliderShapeXz.x, movableData.SelfColliderShapeXz.y) *
//                         SqueezeColliderDetectionRatio,
//                         transform.Position,
//                         ObstacleLayerMask,
//                         DetectRayBelongsTo,
//                         direction,
//                         moveDelta, out var hitEntity))
//                 {
//                     // Can only pass squeeze data to ally unit 
//                     if (InteractAttrLookup.TryGetComponent(hitEntity, out var interactableAttr)
//                         && interactableAttr is { FactionTag: FactionTag.Ally, BaseTag: BaseTag.Units }
//                        )
//                     {
//                         ECB.AddComponent(index, hitEntity, new SqueezeData
//                         {
//                             MoveVector = data.MoveVector
//                         });
//                     }
//                     else
//                     {
//                         // Fail to squeeze, that way has something unable to move
//                         ECB.RemoveComponent<SqueezeData>(index, entity);
//                         return;
//                     }
//                 }
//
//                 transform.Position += data.MoveVector;
//                 ECB.RemoveComponent<SqueezeData>(index, entity);
//             }
//         }
//     }
// }
// using Unity.Entities;
// using Unity.Burst;
// using Unity.Transforms;
// using Unity.Mathematics;
// using Unity.Collections;
// using Unity.Physics;
// using SparFlame.GamePlaySystem.General;
// using SparFlame.GamePlaySystem.UnitSelection;
//
// namespace SparFlame.GamePlaySystem.Movement
// {
//     [BurstCompile]
//     [UpdateBefore(typeof(TransformSystemGroup))]
//     public partial struct AutoGiveWaySystem : ISystem
//     {
//         private ComponentLookup<InteractableAttr> _interactAttrLookup;
//         private ComponentLookup<Selected> _selectedLookup;
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<MovementConfig>();
//             state.RequireForUpdate<PhysicsWorldSingleton>();
//             state.RequireForUpdate<NotPauseTag>();
//             state.RequireForUpdate<AutoGiveWaySystemConfig>();
//             _interactAttrLookup = state.GetComponentLookup<InteractableAttr>(true);
//             _selectedLookup = state.GetComponentLookup<Selected>(true);
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
//             _interactAttrLookup.Update(ref state);
//             _selectedLookup.Update(ref state);
//             var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//             var config = SystemAPI.GetSingleton<AutoGiveWaySystemConfig>();
//             var movementConfig = SystemAPI.GetSingleton<MovementConfig>();
//             var movementSystemConfig = SystemAPI.GetSingleton<MovementConfig>();
//             
//             new AutoGiveWayJob
//             {
//                 PhysicsWorld = physicsWorld,
//                 InteractAttrLookup = _interactAttrLookup,
//                 SelectedLookup = _selectedLookup,
//                 ECB = ecb.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                 Duration = config.Duration,
//                 DeltaTime = SystemAPI.Time.DeltaTime,
//                 ObstacleLayerMask = movementConfig.ObstacleLayerMask,
//                 DetectRayBelongsTo = movementConfig.DetectRaycasstBelongsTo,
//                 RotationSpeed = movementSystemConfig.RotationSpeed
//             }.ScheduleParallel();
//         }
//
//         [BurstCompile]
//         public void OnDestroy(ref SystemState state)
//         {
//         }
//
//         [BurstCompile]
//         public partial struct AutoGiveWayJob : IJobEntity
//         {
//             [ReadOnly] public PhysicsWorldSingleton PhysicsWorld;
//             [ReadOnly] public ComponentLookup<InteractableAttr> InteractAttrLookup;
//             [ReadOnly] public ComponentLookup<Selected> SelectedLookup;
//             public EntityCommandBuffer.ParallelWriter ECB;
//             [ReadOnly] public float Duration;
//             [ReadOnly] public float DeltaTime;
//             [ReadOnly] public uint ObstacleLayerMask;
//             [ReadOnly] public uint DetectRayBelongsTo;
//             [ReadOnly] public float RotationSpeed;
//
//             private void Execute([ChunkIndexInQuery] int index, ref LocalTransform transform, ref AutoGiveWayData data, ref MovableData movableData,
//                 Entity entity
//             )
//             {
//                 // Only the unselected ally unit will move
//                 if (SelectedLookup.IsComponentEnabled(entity))
//                 {
//                     ECB.RemoveComponent<AutoGiveWayData>(index,entity);
//                     movableData.MovementState = MovementState.NotMoving;
//                     movableData.DetailInfo = DetailInfo.None;
//                     return;
//                 }
//                 
//                 var progress = math.saturate(data.ElapsedTime / Duration);
//                 var moveDelta = math.length(data.MoveVector);
//
//                 // Only when moveDelta > 0.1f, will try give way
//                 if (moveDelta < 0.1f)
//                 {
//                     ECB.RemoveComponent<AutoGiveWayData>(index,entity);
//                     return;
//                 }
//                 var direction = math.normalize(data.MoveVector);
//                 
//                 // Only first time movement will pass the data , and detect the obstacle. As long as unit enter auto give way mode, it will ignore collider
//                 if (data is { ElapsedTime: 0, IfGoBack: false })
//                 {
//                     // Detect if it can give way. Can only give way when front is nothing or front is unselected ally unit
//                     if (MovementUtils.ObstacleInDirection(ref PhysicsWorld, 
//                             math.max(movableData.SelfColliderShapeXz.x, movableData.SelfColliderShapeXz.y),
//                             transform.Position,
//                             ObstacleLayerMask,
//                             DetectRayBelongsTo, 
//                             direction,
//                             moveDelta, out var hitEntity))
//                     {
//                         // Try pass data to unselected ally unit in the way
//                         if (InteractAttrLookup.TryGetComponent(hitEntity, out var interactableAttr)
//                             && interactableAttr is { FactionTag: FactionTag.Ally, BaseTag: BaseTag.Units }
//                             && !SelectedLookup.IsComponentEnabled(hitEntity))
//                         {
//                             ECB.AddComponent(index,hitEntity, new AutoGiveWayData
//                             {
//                                 ElapsedTime = 0f,
//                                 MoveVector = data.MoveVector
//                             });
//                         }
//                         else
//                         {
//                             // Fail to give way, that way has something unable to move
//                             ECB.RemoveComponent<AutoGiveWayData>(index,entity);
//                             return;
//                         }
//                     }
//
//                     // Enter auto give way mode
//                     movableData.MovementState = MovementState.IsMoving;
//                     movableData.DetailInfo = DetailInfo.AutoGiveWay;
//                     
//                 }
//
//                 // Move gradually
//                 var targetRotation = quaternion.LookRotationSafe(-direction, math.up());
//                 transform.Rotation = math.slerp(transform.Rotation.value, targetRotation, DeltaTime * RotationSpeed);
//                 var moveStep = data.MoveVector * (DeltaTime / Duration);
//                 transform.Position += moveStep;
//                 data.ElapsedTime += DeltaTime;
//                 switch (progress)
//                 {
//                     case >= 1.0f when !data.IfGoBack:
//                         data.ElapsedTime = 0f;
//                         data.MoveVector = -data.MoveVector;
//                         data.IfGoBack = true;
//                         return;
//                     case >= 1.0f when data.IfGoBack:
//                         // Exit auto give way mode
//                         movableData.MovementState = MovementState.NotMoving;
//                         movableData.DetailInfo = DetailInfo.None;
//                         ECB.RemoveComponent<AutoGiveWayData>(index,entity);
//                         break;
//                 }
//             }
//             
//             
//             
//         }
//     }
// }
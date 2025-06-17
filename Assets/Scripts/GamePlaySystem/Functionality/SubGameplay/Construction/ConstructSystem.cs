using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.CustomParticleSystem;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Stateful;
using Unity.Rendering;
using Unity.Transforms;
using BoxCollider = Unity.Physics.BoxCollider;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
namespace SparFlame.GamePlaySystem.Building
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial struct ConstructSystem : ISystem
    {

        private BufferLookup<CostList> _costLookup;

        private EntityQuery _buildingQuery;
        private EntityQuery _playerBaseQuery;
        private NativeList<Entity> _grids;
        private ComponentLookup<ConstructableTag> _constructableLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<ConstructSystemConfig>();
            state.RequireForUpdate<PlayerFactionData>();
            state.RequireForUpdate<EnemyResourceDataTag>();
            state.RequireForUpdate<AllyResourceDataTag>();
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<GameStatusData>();
            state.RequireForUpdate<InputMouseData>();
            state.RequireForUpdate<ConstructSystemPrefabs>();
            state.RequireForUpdate<ConstructCommandData>();
            _constructableLookup = state.GetComponentLookup<ConstructableTag>(true);
            _costLookup = state.GetBufferLookup<CostList>(true);

            _playerBaseQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<PlayerTag>()
                .WithAll<CrystalDef>().Build();
            _buildingQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform>().WithAll<BuildingAttr>()
                .WithAll<SubGameplayGeneralAttr>()
                .WithAll<PlayerTag>().Build();
            _grids = new NativeList<Entity>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_grids.IsCreated)
                _grids.Dispose();
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;

            if (gameStatusData == GameStatus.Init)
            {
                data.Faction = SystemAPI.GetSingleton<PlayerFactionData>().Value;
                data.CommandType = ConstructCommandType.None;
                return;
            }

            _constructableLookup.Update(ref state);
            _costLookup.Update(ref state);


            if (data.EnterConstruct && _grids.Length == 0 && !_buildingQuery.IsEmpty)
            {
                VisualizeGrid(ref state);
            }

            if (!data.EnterConstruct && _grids.Length != 0)
            {
                ClearGrid(ref state);
            }

            if (_playerBaseQuery.IsEmpty || data.CommandType == ConstructCommandType.None) return;


            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var playerBaseTrans = _playerBaseQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            
            CheckConstructionCommand(ref state,
                ecb, playerBaseTrans);
            playerBaseTrans.Dispose();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        private void CheckConstructionCommand(ref SystemState state,
            EntityCommandBuffer ecb,
            in NativeArray<LocalTransform> playerBaseTrans)
        {
            var gridSize = SystemAPI.GetSingleton<ConstructSystemConfig>().ConstructionGridSize;
            var prefabs = SystemAPI.GetSingleton<ConstructSystemPrefabs>();
            var customInputData = SystemAPI.GetSingleton<InputMouseData>();
            var allyResourceData =
                SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(SystemAPI.GetSingletonEntity<AllyResourceDataTag>());
            var enemyResourceData =
                SystemAPI.GetBuffer<ResourceTypeToAvailableAmount>(SystemAPI
                    .GetSingletonEntity<EnemyResourceDataTag>());
            ref var data = ref SystemAPI.GetSingletonRW<ConstructCommandData>().ValueRW;
            var resourceData = data.Faction == FactionTag.Ally ? allyResourceData : enemyResourceData;
            var generalAttr = SystemAPI.GetComponent<SubGameplayGeneralAttr>(data.TargetBuilding);
            var buildingAttr = SystemAPI.GetComponent<BuildingAttr>(data.TargetBuilding);
     
            var curFaction = SystemAPI.GetSingleton<PlayerFactionData>().Value;

            // Light faction can only build buildings in light ness, including beacon
       
            
            switch (data.CommandType)
            {
                case ConstructCommandType.Drag:
                    var valid = true;

                    // Check if resource is available
                    if (!data.IsMovementShow)
                    {
                        foreach (var cost in _costLookup[data.TargetBuilding])
                        {
                            if (resourceData[(int)cost.Type].Amount < cost.Amount)
                            {
                                SwitchBuildingState(ref state, ref data, PlacementStateType.NotEnoughResources,
                                    in prefabs, false);
                                valid = false;
                            }
                        }
                    }

                    // Check if overlap with other colliders
                    var events = SystemAPI.GetBuffer<StatefulTriggerEvent>(data.GhostTriggerEntity);
                    if (events.Length > 0)
                    {
                        SwitchBuildingState(ref state, ref data, PlacementStateType.Overlapping, in prefabs, false);
                        valid = false;
                    }

                    // Dark crystal can construct anywhere except for light faction tile

                    // If not on constructable plane or this place is occupied by enemy then not constructable
                    if (!_constructableLookup.HasComponent(customInputData.HitEntity) || !_constructableLookup.IsComponentEnabled(customInputData.HitEntity))
                    {
                        SwitchBuildingState(ref state, ref data, PlacementStateType.NotConstructable, in prefabs,
                            false);
                        valid = false;
                    }
                  
             
                    if (valid)
                        SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in prefabs, false);

                    // Synchronize the position and rotation of ghost building and ghost trigger with the input position
                    ref var ghostTransform =
                        ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostModelEntity).ValueRW;
                    ref var triggerTransform =
                        ref SystemAPI.GetComponentRW<LocalTransform>(data.GhostTriggerEntity).ValueRW;

                    ref var cubePreviewTransform =
                        ref SystemAPI.GetComponentRW<LocalTransform>(data.PreviewCube).ValueRW;
                    // Get Target Transform
                    var targetTransform = ghostTransform;
                    
                    var rotationDelta = quaternion.RotateY(math.radians(data.RotationAngle));
                    targetTransform.Scale = 1;
                    targetTransform.Rotation =
                        math.normalizesafe(math.mul(targetTransform.Rotation, rotationDelta));
                    var rotationAbsAngle = ConstructUtils.GetCurrentYDeg(targetTransform.Rotation);
                    GeneralUtils.GetSnapGridPosition(customInputData.HitPosition,rotationAbsAngle , generalAttr.BoxColliderSize,
                        gridSize, out var snapPosition);
                    // targetTransform.Position = customInputData.HitPosition;
                    targetTransform.Position = snapPosition;

                    ghostTransform = targetTransform;
                    triggerTransform = targetTransform;
                    if (data.PreviewAttackRangeEntity != Entity.Null)
                    {
                        ref var attackPreviewTransform =
                            ref SystemAPI.GetComponentRW<LocalTransform>(data.PreviewAttackRangeEntity).ValueRW;
                        attackPreviewTransform.Position = targetTransform.Position;
                        attackPreviewTransform.Rotation = targetTransform.Rotation;
                    }

                    cubePreviewTransform.Position = targetTransform.Position;
                    cubePreviewTransform.Rotation = targetTransform.Rotation;
                    break;

                case ConstructCommandType.Start:
                    // Check if switch building, then should destroy prior ghost preview
                    if (data.GhostModelEntity != Entity.Null)
                    {
                        DestroyPriorGhost(ref state, ref data);
                        ClearPreview(ref state, ref data);
                    }

                    // Create ghost preview
                    data.GhostModelEntity = InstantiateChildrenWithNewParent(ref state, data.TargetBuilding);
                    data.GhostTriggerEntity = state.EntityManager.Instantiate(prefabs.GhostTriggerPrefab);
                    if (SystemAPI.HasComponent<AttackStateTag>(data.TargetBuilding))
                    {
                        data.PreviewAttackRangeEntity =
                            state.EntityManager.Instantiate(prefabs.PreviewAttackRangePrefab);
                        var ability = SystemAPI.GetComponent<AttackAbility>(data.TargetBuilding);
                        state.EntityManager.SetComponentData(data.PreviewAttackRangeEntity, new LocalTransform
                        {
                            Scale = math.sqrt(ability.Range)
                        });
                    }
                    else data.PreviewAttackRangeEntity = Entity.Null;


                    GetPreviewCube(ref state, prefabs.PreviewCubePrefab, generalAttr.BoxColliderSize, gridSize,
                        out data.PreviewCube);
                    // VisualizeGrid(ref state, gridSize, playerBaseTrans,constructableRadiusSq, prefabs.GridPrefab);

                    state.EntityManager.AddComponent<SubGameplayEntityTag>(data.GhostModelEntity);
                    state.EntityManager.AddComponent<SubGameplayEntityTag>(data.GhostTriggerEntity);
                    SwitchBuildingState(ref state, ref data, PlacementStateType.Valid, in prefabs, true);
                    AlignTriggerBoxCollider(ref state, in data);
                    data.CommandType = ConstructCommandType.Drag;
                    break;

                case ConstructCommandType.End:
                    if (data.IsMovementShow) // Not move to new place, should return to original location
                    {
                        state.EntityManager.SetComponentData(data.TargetBuilding, data.OriTransform);
                    }
                    
                    ClearPreview(ref state, ref data);
                    DestroyPriorGhost(ref state, ref data);
                    data.CommandType = ConstructCommandType.None;
                    break;

                case ConstructCommandType.Build when data.State == PlacementStateType.Valid:
                    var newTransform = SystemAPI.GetComponent<LocalTransform>(data.GhostModelEntity);

                    if (!data.IsMovementShow)
                    {
                        // Reduce resources
                        foreach (var cost in _costLookup[data.TargetBuilding])
                        {
                            var r = resourceData[(int)cost.Type];
                            r.Amount -= cost.Amount;
                            resourceData[(int)cost.Type] = r;
                        }

                        // Create building
                        var targetBuilding = state.EntityManager.Instantiate(data.TargetBuilding);
                        state.EntityManager.AddComponent<SubGameplayEntityTag>(targetBuilding);
                        state.EntityManager.SetComponentData(targetBuilding, newTransform);

                        // Make the building in constructing state
                        state.EntityManager.AddComponent<ConstructingData>(targetBuilding);
                        var attr = state.EntityManager.GetComponentData<BuildingAttr>(targetBuilding);
                        state.EntityManager.SetComponentData(targetBuilding, new ConstructingData
                        {
                            LastTime = attr.ConstructTime
                        });
                        if(buildingAttr.Type == BuildingType.Dwellings)
                            state.EntityManager.SetComponentEnabled<DwellingGeneratePopulationTag>(targetBuilding,false);
                        state.EntityManager.SetComponentEnabled<VolumeObstacleSpawnRequest>(targetBuilding,false);
                        
                        
                        // Exchange grid preview
                        _grids.Add(data.PreviewCube);
                        GetPreviewCube(ref state, prefabs.PreviewCubePrefab, generalAttr.BoxColliderSize, gridSize,
                            out data.PreviewCube);
                        
                        data.CommandType = ConstructCommandType.Drag; // Continue building
                        
                        var vfxRequest = ecb.CreateEntity();
                        ecb.AddComponent<SubGameplayEntityTag>(vfxRequest);
                        ecb.AddComponent(vfxRequest, new VFXRequest
                        {
                            ParabolaTargetPosition = default,
                            Filter = new VFXSubFilter
                            {
                                Faction = curFaction,
                                FactionFilterEnable = true,
                                Tier = default,
                                TierFilterEnable = false
                            },
                            KeepDuration = 0,
                            SpawnPosition = newTransform.Position,
                            StatChangeRequest = default,
                            RequestType = VFXRequestType.Spawn,
                            VFXName = VFXName.Construct,
                            VFXTrackTarget = Entity.Null
                        });

                     
                    }
                    else
                    {
                        // Move building to new place
                        _grids.Add(data.PreviewCube);
                        state.EntityManager.SetComponentData(data.TargetBuilding, newTransform);
                        DestroyPriorGhost(ref state, ref data);
                        data.CommandType = ConstructCommandType.None;
                        var syncVolumeRequest = state.EntityManager.CreateEntity();
                        state.EntityManager.AddComponent<BuildingSyncVolumeRequest>(syncVolumeRequest);
                        state.EntityManager.SetComponentData(syncVolumeRequest, new BuildingSyncVolumeRequest
                        {
                            FromEntity = data.TargetBuilding,
                        });
                        state.EntityManager.AddComponent<SubGameplayEntityTag>(syncVolumeRequest);
                    }

                    break;
                case ConstructCommandType.None:
                    // Do nothing when not enter ghost show mode
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ClearPreview(ref SystemState state, ref ConstructCommandData data)
        {
           
            if (data.PreviewCube != Entity.Null)
                state.EntityManager.DestroyEntity(data.PreviewCube);
            if (data.PreviewAttackRangeEntity != Entity.Null)
                state.EntityManager.DestroyEntity(data.PreviewAttackRangeEntity);
        }

        private void ClearGrid(ref SystemState state)
        {
            foreach (var entity in _grids)
            {
                state.EntityManager.DestroyEntity(entity);
            }

            _grids.Clear();
        }

        private void GetPreviewCube(ref SystemState state, Entity cubePrefab, float3 targetColliderSize, float gridSize,
            out Entity cubeParent)
        {
            var entityManager = state.EntityManager;

            cubeParent = entityManager.CreateEntity();

            state.EntityManager.AddComponent<SubGameplayEntityTag>(cubeParent);
            state.EntityManager.AddComponent<LocalTransform>(cubeParent);
            state.EntityManager.AddComponent<LocalToWorld>(cubeParent);
            var buffer = state.EntityManager.AddBuffer<LinkedEntityGroup>(cubeParent);
            entityManager.SetComponentData(cubeParent, new LocalTransform
            {
                Position = float3.zero,
                Rotation = quaternion.identity,
                Scale = 1f
            });
            buffer.Add(cubeParent);

            entityManager.AddComponent<SubGameplayEntityTag>(cubeParent);
            // _cubesAndParent.Add(cubeParent);
            // 2. 计算在网格下所需的格子数量
            int sizeX = (int)math.ceil(targetColliderSize.x / gridSize);
            int sizeZ = (int)math.ceil(targetColliderSize.z / gridSize);

            // 3. 计算中心偏移，使 cubes 居中于 parent
            float offsetX = -(sizeX - 1) * gridSize * 0.5f;
            float offsetZ = -(sizeZ - 1) * gridSize * 0.5f;

            // 4. 逐个生成 cube，并设置相对于 parent 的位置
            for (int x = 0; x < sizeX; x++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    float3 localPos = new float3(
                        offsetX + x * gridSize,
                        0f,
                        offsetZ + z * gridSize
                    );
                    var cubeEntity = entityManager.Instantiate(cubePrefab);
                    entityManager.SetComponentData(cubeEntity, new LocalTransform
                    {
                        Position = localPos,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    // 设置为 cubeParent 的子物体
                    entityManager.AddComponentData(cubeEntity, new Parent { Value = cubeParent });
                    // _cubesAndParent.Add(cubeEntity);
                    var buffer2 = SystemAPI.GetBuffer<LinkedEntityGroup>(cubeParent);
                    buffer2.Add(cubeEntity);
                }
            }
        }

   

        private void VisualizeGrid(ref SystemState state)
        {
            var prefabs = SystemAPI.GetSingleton<ConstructSystemPrefabs>();
            var gridSize = SystemAPI.GetSingleton<ConstructSystemConfig>().ConstructionGridSize;
            var trans = _buildingQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var generalAttrs = _buildingQuery.ToComponentDataArray<SubGameplayGeneralAttr>(Allocator.Temp);

            for (var i = 0; i < trans.Length; i++)
            {
                var boxColliderSize = generalAttrs[i].BoxColliderSize;
                var tran = trans[i];

                // 1. 获取旋转角度（只支持 90° 的倍数）
                int yRotDeg = ConstructUtils.GetYRotation90FromQuaternion(tran.Rotation);

                // 2. 生成 cubeParent + 子 cubes（旋转支持）
                GetPreviewCube(ref state, prefabs.PreviewCubePrefab, boxColliderSize, gridSize,
                    out var cubeParent);

                // 3. 计算吸附位置（根据旋转决定对齐）
                GeneralUtils.GetSnapGridPosition(tran.Position, yRotDeg , boxColliderSize, gridSize, out var gridPosition);

                // 4. 设置 cubeParent 的位置和旋转
                state.EntityManager.SetComponentData(cubeParent, new LocalTransform
                {
                    Position = gridPosition,
                    Rotation = tran.Rotation,
                    Scale = 1f
                });

                state.EntityManager.AddComponent<SubGameplayEntityTag>(cubeParent);
                _grids.Add(cubeParent);
            }
        }



        private void DestroyPriorGhost(ref SystemState state, ref ConstructCommandData data)
        {
            state.EntityManager.DestroyEntity(data.GhostModelEntity);
            state.EntityManager.DestroyEntity(data.GhostTriggerEntity);
            data.GhostModelEntity = Entity.Null;
            data.GhostTriggerEntity = Entity.Null;
            data.CommandType = ConstructCommandType.Drag;
        }

        private void SwitchBuildingState(ref SystemState state, ref ConstructCommandData data,
            in PlacementStateType targetState, in ConstructSystemPrefabs prefabs, bool force)
        {
            if (targetState == data.State && !force) return;
            data.State = targetState;
            var targetMaterial = targetState switch
            {
                PlacementStateType.Valid => SystemAPI.GetComponent<MaterialMeshInfo>(prefabs.ValidPreset).Material,
                PlacementStateType.Overlapping => SystemAPI.GetComponent<MaterialMeshInfo>(prefabs.OverlappingPreset)
                    .Material,
                PlacementStateType.NotEnoughResources => SystemAPI
                    .GetComponent<MaterialMeshInfo>(prefabs.NotEnoughResourcesPreset)
                    .Material,
                PlacementStateType.NotConstructable => SystemAPI
                    .GetComponent<MaterialMeshInfo>(prefabs.NotConstructablePreset).Material,
                _ => throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null)
            };
            // for (int i = 1; i < buffer.Length; i++)
            // {
            ChangeMaterialRecursively(ref state, data.PreviewCube, targetMaterial);
            // }
        }
        private void ChangeMaterialRecursively(ref SystemState state, Entity entity, int newMaterial)
        {
            var buffer = SystemAPI.GetBuffer<LinkedEntityGroup>(entity);
            foreach (var group in buffer)
            {
                if (SystemAPI.HasComponent<MaterialMeshInfo>(group.Value))
                {
                    var material = SystemAPI.GetComponentRW<MaterialMeshInfo>(group.Value);
                    material.ValueRW.Material = newMaterial;
                }
                // if (SystemAPI.ManagedAPI.HasComponent<ParticleSystem>(group.Value))
                // {
                //     var sys = SystemAPI.ManagedAPI.GetComponent<ParticleSystem>(group.Value);
                //     switch (targetState)
                //     {
                //         case PlacementStateType.Valid:
                //             sys.set
                //             break;
                //         case PlacementStateType.Overlapping:
                //             break;
                //         case PlacementStateType.NotEnoughResources:
                //             break;
                //         case PlacementStateType.NotConstructable:
                //             break;
                //         default:
                //             throw new ArgumentOutOfRangeException(nameof(targetState), targetState, null);
                //     }
                // }
            }
           
            // if (!SystemAPI.HasBuffer<LinkedEntityGroup>(entity)) return;
            // for (int i = 1; i < buffer.Length; i++)
            // {
            //     ChangeMaterialRecursively(ref state, buffer[i].Value, newMaterial);
            // }
        }

        private void AlignTriggerBoxCollider(ref SystemState state, in ConstructCommandData data)
        {
            var targetCollider = SystemAPI.GetComponent<PhysicsCollider>(data.TargetBuilding);
            var ghostTriggerCollider = SystemAPI.GetComponentRW<PhysicsCollider>(data.GhostTriggerEntity);
            unsafe
            {
                var bxPtr = (BoxCollider*)targetCollider.ColliderPtr;
                var targetBox = bxPtr->Geometry;
                bxPtr = (BoxCollider*)ghostTriggerCollider.ValueRW.ColliderPtr;
                bxPtr->Geometry = targetBox;
            }
        }


        private Entity InstantiateChildrenWithNewParent(ref SystemState state, Entity oriParentEntity)
        {
            if (!SystemAPI.HasBuffer<LinkedEntityGroup>(oriParentEntity))
                return Entity.Null;

            var linkedEntities = SystemAPI.GetBuffer<LinkedEntityGroup>(oriParentEntity);
            if (linkedEntities.Length <= 1)
                return Entity.Null;

            using var originalChildren = new NativeList<Entity>(linkedEntities.Length - 1, Allocator.Temp);

            for (var i = 1; i < linkedEntities.Length; i++)
            {
                originalChildren.Add(linkedEntities[i].Value);
            }

            // Create new parent
            var newParentEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponent<SubGameplayEntityTag>(newParentEntity);

            state.EntityManager.AddComponent<LocalTransform>(newParentEntity);
            state.EntityManager.AddComponent<LocalToWorld>(newParentEntity);
            var buffer = state.EntityManager.AddBuffer<LinkedEntityGroup>(newParentEntity);
            buffer.Add(newParentEntity);

            // Get original parent world transform
            var bLtw = state.EntityManager.GetComponentData<LocalToWorld>(oriParentEntity);
            var bLtwInverse = math.inverse(bLtw.Value);

            foreach (var originalChild in originalChildren)
            {
                var newChild = state.EntityManager.Instantiate(originalChild);
                state.EntityManager.AddComponent<SubGameplayEntityTag>(newChild);

                var childLtw = state.EntityManager.GetComponentData<LocalToWorld>(originalChild);
                var relativeToB = math.mul(bLtwInverse, childLtw.Value);

                // extract position
                float3 position = relativeToB.c3.xyz;

                // extract scale
                float3 scale;
                scale.x = math.length(relativeToB.c0.xyz);
                scale.y = math.length(relativeToB.c1.xyz);
                scale.z = math.length(relativeToB.c2.xyz);

                // normalize basis vectors to remove scale from rotation
                float3x3 rotationMatrix = new float3x3(
                    relativeToB.c0.xyz / scale.x,
                    relativeToB.c1.xyz / scale.y,
                    relativeToB.c2.xyz / scale.z
                );
                quaternion rotation = new quaternion(rotationMatrix);

                // Calculate new transform
                var newLocalTransform = new LocalTransform
                {
                    Position = position,
                    Rotation = rotation,
                    Scale = math.cmax(scale)
                };

                state.EntityManager.SetComponentData(newChild, newLocalTransform);
                state.EntityManager.SetComponentData(newChild, new Parent { Value = newParentEntity });
                var newLinkedEntities = state.EntityManager.GetBuffer<LinkedEntityGroup>(newParentEntity);
                newLinkedEntities.Add(new LinkedEntityGroup { Value = newChild });
            }

            return newParentEntity;
        }

 

        // private void VisualizeGrid(ref SystemState state,
        //     float gridSize,
        //     NativeArray<LocalTransform> basePositions, float constructableRadiusSq,
        //     Entity gridPreviewPrefab)
        // {
        //     var entityManager = state.EntityManager;
        //
        //     foreach (var trans in basePositions)
        //     {
        //         var basePos = trans.Position;
        //         int radiusInGrid = (int)math.ceil(math.sqrt(constructableRadiusSq)) / (int)gridSize;
        //         for (int x = -radiusInGrid; x <= radiusInGrid; x++)
        //         {
        //             for (int z = -radiusInGrid; z <= radiusInGrid; z++)
        //             {
        //                 float3 pos = basePos + new float3(x * gridSize, 0, z * gridSize);
        //                 if (math.distancesq(pos, basePos) > constructableRadiusSq)
        //                     continue;
        //
        //                 var gridEntity = entityManager.Instantiate(gridPreviewPrefab);
        //                 entityManager.AddComponent<GameplayEntityTag>(gridEntity);
        //                 entityManager.SetComponentData(gridEntity, new LocalTransform
        //                 {
        //                     Position = pos,
        //                     Rotation = quaternion.identity,
        //                     Scale = 1
        //                 });
        //                 entityManager.AddComponent<TestPreview>(gridEntity);
        //                 _grids.Add(gridEntity);
        //             }
        //         }
        //     }
        // }
    }
}
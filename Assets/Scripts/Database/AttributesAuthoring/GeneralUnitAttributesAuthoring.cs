using SparFlame.GamePlaySystem.CameraControl;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;
using UnityEngine.AI;

namespace SparFlame.Database
{
    public class GeneralUnitAttributesAuthoring : GeneralDataItemAuthoring
    {
        private class Baker : GeneralDataItemBaker<GeneralUnitAttributesAuthoring>
        {
            public override void Bake(GeneralUnitAttributesAuthoring authoring)
            {
                if (authoring.globalIdx == 0)
                {
                    return;
                }
                var item = DatabaseManager.UnitDatabaseSo.GetItemById(authoring.globalIdx);
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                BakeGeneralDataItem(entity,item);
                AddComponent<GarrisonStateTag>(entity);
                SetComponentEnabled<GarrisonStateTag>(entity, false);
                AddComponent(entity, new UnitAttr
                {
                    Type = item.type,
                    SubTypeIndex = item.GetSubtypeIndex(),
                    ConjureSpeedSecondPerUnit = item.conjureSpeedSecondPerUnit
                });
                var buffer = AddBuffer<CostList>(entity);
                foreach (var cost in item.costs)
                {
                    buffer.Add(new CostList
                    {
                        Amount = cost.amount,
                        Type = cost.costResourceType
                    });
                }
                BakeMovementAttr( item,entity,authoring);
                BakeSelectableAttr(item,entity);
                BakeAttunerAttr( item,entity);
            }
            
            
            
            private void BakeMovementAttr(UnitDataItem item,Entity entity, GeneralUnitAttributesAuthoring authoring)
            {
                AddComponent(entity, new NavAgentComponent
                {
                    TargetPosition = float3.zero,
                    CalculateInterval = item.movementCalculationInterval,
                    Extents = float3.zero,
                    EnableCalculation = false,
                    CalculationComplete = false,
                    CurrentWaypoint = 0,
                    ForceCalculate = false,
                    AgentId = authoring.GetComponent<NavMeshAgent>().agentTypeID
                });

                var physicsShape = item.prefab.GetComponent<PhysicsShapeAuthoring>();
                AddComponent(entity, new MovableData
                {
                    MoveSpeed = item.moveSpeed,
                    TargetCenterPos = float3.zero,
                    TargetColliderShapeXZ = float2.zero,
                    MovementCommandType = MovementCommandType.None,
                    InteractiveRangeSq = 0f,
                    DetailInfo = DetailInfo.None,
                    MovementState = MovementState.NotMoving,
                    ForceCalculate = false,
                    SelfColliderShapeXz = new float2(physicsShape.m_PrimitiveSize.x,physicsShape.m_PrimitiveSize.z),
                });
                AddComponent(entity, new Surroundings
                {
                    MoveSuccess = true,
                    FrontEntity = Entity.Null,
                    LeftEntity = Entity.Null,
                    RightEntity = Entity.Null,
                });
                AddBuffer<WaypointBuffer>(entity);
                AddComponent<MovingStateTag>(entity);
                SetComponentEnabled<MovingStateTag>(entity, false);
            }

            private void BakeSelectableAttr(UnitDataItem item,Entity entity)
            {
                AddComponent<Selected>(entity);
                SetComponentEnabled<Selected>(entity, false);
                AddComponent<LockSelectedWorkForDrag>(entity);
                SetComponentEnabled<LockSelectedWorkForDrag>(entity, false);
            }

            private void BakeAttunerAttr( UnitDataItem item,Entity entity)
            {
                if (item is WorkerData workerData)
                {
                    if (workerData.workerType == WorkerType.Attuner)
                    {
                        AddComponent(entity, new AttunerAttr
                        {
                            GenerateSpeedBonus = workerData.generateSpeedBonus
                        });
                    }
                }
            }
        }
    }
}
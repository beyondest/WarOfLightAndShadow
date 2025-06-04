using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
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
                    ConjureSpeedSecondPerUnit = item.conjureSpeedSecondPerUnit,
                    AnimatedModelIndex = item.animatedRootIndex
                });
                switch (item.type)
                {
                    case UnitType.Shield:
                        AddComponent<ShieldTag>(entity);
                        break;
                    case UnitType.Ranged:
                        AddComponent<RangedTag>(entity);

                        break;
                    case UnitType.Magic:
                        if(item.GetSubtypeIndex() == (int)MagicType.Cleric)
                            AddComponent<ClericTag>(entity);
                        if(item.GetSubtypeIndex() == (int)MagicType.Mage)
                            AddComponent<MageTag>(entity);
                        break;
                    case UnitType.Cavalry:
                        AddComponent<CavalryTag>(entity);
                        break;
                    case UnitType.Worker:
                        AddComponent<WorkerTag>(entity);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
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

          
        }
    }
}
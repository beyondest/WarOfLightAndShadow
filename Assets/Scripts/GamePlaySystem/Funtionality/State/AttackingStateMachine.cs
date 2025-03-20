using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Interact;
using Unity.Mathematics;


// TODO : Use generic system/job to implement the attack/heal/harvest system
namespace SparFlame.GamePlaySystem.State
{
    [UpdateAfter(typeof(SortInsightTargetSysetm))]
    [UpdateAfter(typeof(HealthSystem))]
    public partial struct AttackingStateMachine : ISystem
    {
        public partial struct AttackJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            public ComponentLookup<HealthData> HealthData;
            public ComponentLookup<MovableData> MovableData;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            [ReadOnly] public BufferLookup<InsightTarget> TargetList;

            public float DeltaTime;

            private void Execute([ChunkIndexInQuery] int index, ref UnitBasicStateData stateData,
                ref MovableData movableData,
                ref AttackAbility ability,
                Entity entity)
            {
                if (!TargetList.TryGetBuffer(entity, out var targetList)) return;
                // Current target is not alive
                if (!IsTargetAlive(ref stateData))
                {
                    // No enemy in sight or memory target not in sight
                    if (targetList.IsEmpty || (stateData.Focus && targetList.Contains(new InsightTarget
                        {
                            Target = stateData.MemoryEntity
                        })))
                    {
                        StateUtils.ContinueLastCommand(ref stateData, ECB, entity, index);
                        return;
                    }

                    // No enemy around and no memory command, turn to idle
                    if (targetList.IsEmpty)
                    {
                        stateData.TargetState = UnitState.Idle;
                        StateUtils.SwitchState(ref stateData, ECB, entity, index);
                        return;
                    }
                    // Enemy in sight and no memory command, choose the highest value target
                    else
                    {
                        stateData.TargetEntity = targetList[0].Target;
                        return;
                    }
                }

                var curPos = TransformLookup.GetRefRO(entity).ValueRO.Position;
                var targetPos = TransformLookup.GetRefRO(stateData.TargetEntity).ValueRO.Position;
                
                // Current target is not in range
                if (!IsTargetInRange(ref ability, in curPos, in targetPos))
                {
                    AttackMoveToTarget(ref stateData, ref movableData);
                }

                // Try attack current target                
                if (++ability.CurCounter > (int)1 / DeltaTime / ability.Count)
                {
                    ability.CurCounter = 0;
                    SendDamageDealtRequest(ref stateData, ref ability);
                }
            }

            private void SendDamageDealtRequest(ref UnitBasicStateData stateData, ref AttackAbility ability)
            {
                throw new System.NotImplementedException();
            }

            private void AttackMoveToTarget(ref UnitBasicStateData stateData, ref MovableData movableData)
            {
                stateData.TargetState = UnitState.Moving;
                
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static bool IsTargetInRange(ref AttackAbility ability, in float3 curPos, in float3 targetPos)
            {
                var disSq = math.distancesq(curPos, targetPos);
                return disSq < ability.RangeSq;
            }

            private bool IsTargetAlive(ref UnitBasicStateData stateData)
            {
                if (!HealthData.TryGetComponent(stateData.TargetEntity, out HealthData healthData)) return false;
                return healthData.Value > 0;
            }
        }
    }
}
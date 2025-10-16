using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace SparFlame.Systems.SubGameplay.Movement
{
   [BurstCompile]
    [WithNone(typeof(InGarrison))]
    public partial struct UnitFinalTransformApplyJob : IJobEntity
    {
        [ReadOnly] public CbrConfig Config;
        [ReadOnly] public float DeltaTime;
        [ReadOnly] public MovementDebug Debug;
        [ReadOnly] public ComponentLookup<PlayerTag> PlayerTagLookup;

        private void Execute(
            ref LocalTransform transform,
            ref Velocity velocity,
            in SeekTarget seekTargetOri,
            in Separation separationOri,
            in Avoidance avoidance,
            in MovableData movableData,
            in InteractAbilityBonus bonus,
            in NavAgentComponent agent,
            in GroundInfo groundInfo,
            Entity selfEntity
        )
        {
            // Calculate truly movement direction and apply rotation
            var horizontalSeekTarget = new float3(seekTargetOri.Direction.x, 0, seekTargetOri.Direction.z);
            var seekTarget = new SeekTarget { Direction = float3.zero };
            var separation = new Separation { Value = float3.zero };

            var isSeekingTarget = math.lengthsq(seekTargetOri.Direction) > 0.001f;
            var isBeingPushed = math.lengthsq(separationOri.Value) > 0.001f;
            
            // Correct the seek target direction by ground normal
            if (isSeekingTarget)
            {
                float3 forward = math.normalize(horizontalSeekTarget);
                float3 right = math.normalizesafe(math.cross(groundInfo.Normal, forward));
                forward = math.cross(right, groundInfo.Normal);
                seekTarget.Direction = forward;
            }
            // Correct the separation direction by ground normal
            if (isBeingPushed)
            {
                separation.Value = separationOri.Value -
                                   groundInfo.Normal * math.dot(separationOri.Value, groundInfo.Normal);
            }

            // Calculate max speed
            var scale = PlayerTagLookup.HasComponent(selfEntity) ? Debug.playerMovementScale : Debug.aiMovementScale;
            var debugScale = Debug.enabled ? scale : 1f;
            var maxSpeed = (movableData.MoveSpeed + bonus.MoveSpeedBonus) * debugScale;

            // Calculate target velocity. Use CBR
            var targetV = float3.zero;
            
            if (isSeekingTarget)
            {
                targetV = seekTarget.Direction  * Config.SeekTargetWeight;
                targetV += avoidance.Value * Config.AvoidanceWeight;
                
                // Only turn rotation when seeking target
                var faceDirection = math.normalizesafe(targetV);
                var targetRotation = quaternion.LookRotationSafe(faceDirection, groundInfo.Normal);
                transform.Rotation = math.slerp(
                    transform.Rotation.value,
                    targetRotation,
                    DeltaTime * Config.RotationSpeed);
            }

            if (isBeingPushed)
            {
                targetV += separation.Value * Config.SeparationWeight;
             
            }

            targetV *= maxSpeed;
            var deltaV = targetV - velocity.Value;

            // Clamp acceleration
            if (math.lengthsq(deltaV) > Config.MaxAcceleration * Config.MaxAcceleration)
            {
                deltaV = math.normalizesafe(deltaV) * Config.MaxAcceleration;
            }

            velocity.Value += deltaV;

            // Clamp speed
            if (math.lengthsq(velocity.Value) > maxSpeed * maxSpeed)
            {
                velocity.Value = math.normalizesafe(velocity.Value) * maxSpeed;
            }

            transform.Position += velocity.Value * DeltaTime;
            transform.Position.y = groundInfo.HitPosition.y;

            
        }
    }
}
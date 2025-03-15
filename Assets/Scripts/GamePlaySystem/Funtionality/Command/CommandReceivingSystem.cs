using Unity.Burst;
using Unity.Entities;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.General;
namespace SparFlame.GamePlaySystem.Command
{
    public partial struct CommandReceivingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            
        }
        
        
        [BurstCompile]
        [WithAll(typeof(HaveTarget))]
        public partial struct MovementRecJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery]int index, ref MovableData movableData, in BasicAttr basicAttr,
                Entity entity)
            {
                if (movableData.MovementState == MovementState.MovementComplete)
                {
                    
                }
            }
        }

        
    }
}
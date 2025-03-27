using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;
namespace DefaultNamespace
{
    public partial struct MoveSys : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var trans in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<TestAttr>())
            {
                trans.ValueRW.Position.y -= 0.5f;
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }
    }
}
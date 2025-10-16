// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Jobs;
// using Unity.Mathematics;
// using Unity.Transforms;
//
// // Define a component to hold rotation speed
// public struct RotationSpeed : IComponentData
// {
//     public float RadiansPerSecond;
// }
//
// // Define the IJobChunk job
// [BurstCompile]
// public struct RotationJob : IJobChunk
// {
//     public float DeltaTime;
//
//     // ComponentTypeHandle for read-write access to Rotation
//     public ArchetypeChunkComponentType<Rotation> RotationType;
//     // ComponentTypeHandle for read-only access to RotationSpeed
//     [ReadOnly] public ArchetypeChunkComponentType<RotationSpeed> RotationSpeedType;
//
//     public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
//     {
//         // Get NativeArrays for the components in the current chunk
//         var rotations = chunk.Get NativeArray(RotationType);
//         var rotationSpeeds = chunk.Get NativeArray(RotationSpeedType);
//
//         // Iterate over entities in the chunk
//         for (int i = 0; i < chunk.Count; i++)
//         {
//             var rotation = rotations[i];
//             var rotationSpeed = rotationSpeeds[i];
//
//             // Apply rotation
//             rotation.Value = math.mul(math.normalize(rotation.Value),
//                 quaternion.AxisAngle(math.up(), rotationSpeed.RadiansPerSecond * DeltaTime));
//             rotations[i] = rotation;
//         }
//     }
// }
//
// // Define the SystemBase to schedule the job
// public partial class RotationSystem : SystemBase
// {
//     private EntityQuery m_RotationQuery;
//
//     protected override void OnCreate()
//     {
//         // Define the EntityQuery to select entities with Rotation and RotationSpeed components
//         m_RotationQuery = GetEntityQuery(
//             ComponentType.ReadWrite<Rotation>(),
//             ComponentType.ReadOnly<RotationSpeed>()
//         );
//     }
//
//     protected override void OnUpdate()
//     {
//         // Create an instance of the job
//         var job = new RotationJob
//         {
//             DeltaTime = SystemAPI.Time.DeltaTime,
//             RotationType = GetArchetypeChunkComponentType<Rotation>(false), // false for write access
//             RotationSpeedType = GetArchetypeChunkComponentType<RotationSpeed>(true) // true for read-only access
//         };
//
//         // Schedule the job with the query and dependencies
//         Dependency = job.Schedule(m_RotationQuery, Dependency);
//     }
// }
// using Unity.Entities;
//
// namespace SparFlame.Systems.SubGameplay.Interact
// {
//     // GridVisionSystem.cs
//     using Unity.Burst;
//     using Unity.Collections;
//     using Unity.Collections.LowLevel.Unsafe;
//     using Unity.Entities;
//     using Unity.Jobs;
//     using Unity.Mathematics;
//     using Unity.Transforms;
//     using UnityEngine;
//
//     #region Components
//
// // Tag to indicate this entity should perform sight detection this frame
//     public struct NeedTarget : IComponentData
//     {
//     }
//
// // Faction id (0 = neutral, 1 = teamA, 2 = teamB, etc.)
//     public struct Faction : IComponentData
//     {
//         public int value;
//     }
//
// // Sight range for the entity (in world units)
//     // public struct SightRangeSq : IComponentData
//     // {
//     //     public float value;
//     // }
//
// // Simple alive marker / HP check could be different in your project.
// // We assume if it has Health and health > 0, it's alive. For simplicity use marker:
//     public struct AliveTag : IComponentData
//     {
//     }
//
// // Buffer element to hold insight results
//  
//
//     #endregion
//
//     [UpdateInGroup(typeof(SimulationSystemGroup))]
//     public partial class GridVisionSystem2 : SystemBase
//     {
//         // grid config
//         public float cellSize = 5f; // tune this: smaller -> more cells, larger -> more entities per cell
//
//         protected override void OnCreate()
//         {
//             base.OnCreate();
//         }
//
//         protected override void OnDestroy()
//         {
//             base.OnDestroy();
//         }
//
//         protected override void OnUpdate()
//         {
//             var ecsWorld = World;
//             float cellSizeLocal = math.max(0.001f, cellSize);
//
//             // 1) Count entities we will insert into grid to size containers reasonably
//             EntityQueryBuilder egb = new EntityQueryBuilder(Allocator.Temp);
//             // Query: units that are alive and have position & faction
//             var countQuery = GetEntityQuery(new EntityQueryDesc
//             {
//                 All = new ComponentType[] { typeof(Translation), typeof(Faction), typeof(AliveTag) }
//             });
//
//             int totalUnits = countQuery.CalculateEntityCount();
//
//             // Create the spatial hash: key -> list of entities
//             var mapCapacity = math.max(16, totalUnits * 2);
//             var cellMap = new NativeMultiHashMap<long, Entity>(mapCapacity, Allocator.TempJob);
//
//             // Parallel writer
//             var cellMapWriter = cellMap.AsParallelWriter();
//
//             // Hash function: pack 3 ints into long
//             static long HashCell(int3 c)
//             {
//                 // pack into 64-bit: use 21 bits per coord (good for +/- 1 million cells)
//                 // layout: [unused 1 bit][x:21][y:21][z:21]  (simple, beware overflow if coords huge)
//                 long lx = (long)(c.x & 0x1FFFFF);
//                 long ly = (long)(c.y & 0x1FFFFF);
//                 long lz = (long)(c.z & 0x1FFFFF);
//                 return (lx << 42) | (ly << 21) | lz;
//             }
//
//             // 2) Build the grid in parallel
//             var buildJob = Entities
//                 .WithName("GridBuildJob")
//                 .WithAll<AliveTag>() // only alive units
//                 .ForEach((Entity e, in Translation t, in Faction f) =>
//                 {
//                     float3 pos = t.Value;
//                     int3 cell = (int3)math.floor(pos / cellSizeLocal);
//                     long key = HashCell(cell);
//                     cellMapWriter.Add(key, e);
//                 }).ScheduleParallel(Dependency);
//
//             // 3) Prepare a concurrent queue to collect append operations
//             // AppendOp includes owner (who wants targets) and target found
//             struct AppendOp {
//
//         public Entity owner;
//         public Entity target;
//         public float sqrDist;
//     }
//
//     var appendQueue = new NativeQueue<AppendOp>(Allocator.TempJob);
//     var appendQueueWriter = appendQueue.AsParallelWriter();
//
//     // For reading faction and transform of entities quickly, we will capture relevant FromEntity info
//     // But we can't do BufferFromEntity writes in parallel safely -> we only enqueue AppendOp here.
//
//     // Need Buffer which holds SightRange and Faction for readers. Use component access in ForEach.
//     var cellSizeCapture = cellSizeLocal;
//     
//     // We will iterate all entities that have NeedTarget + SightRange + Translation + Faction + AliveTag
//     // For each, scan neighboring cells within ceil(range/cellSize) and enqueue matches
//     var detectJob = Entities
//         .WithName("VisionDetectJob")
//         .WithAll<NeedTarget, AliveTag>()
//         .ForEach((Entity owner, in Translation t, in SightRangeSq sight, in Faction myFaction) =>
//         {
//             float3 pos = t.Value;
//             float range = math.max(0f, sight.value);
//             float rangeSqr = range * range;
//
//             int radius = (int)math.ceil(range / cellSizeCapture);
//             int3 centerCell = (int3)math.floor(pos / cellSizeCapture);
//
//             // iterate neighbor cells in cubic box (you can optimize to circle cells if desired)
//             for (int z = -radius; z <= radius; ++z)
//             for (int y = -radius; y <= radius; ++y)
//             for (int x = -radius; x <= radius; ++x)
//             {
//                 int3 c = centerCell + new int3(x, y, z);
//                 long key = HashCell(c);
//
//                 // iterate map entries for this cell
//                 NativeMultiHashMapIterator<long> it;
//                 Entity other;
//                 if (cellMap.TryGetFirstValue(key, out other, out it))
//                 {
//                     do
//                     {
//                         if (other == owner) continue; // skip self
//
//                         // Access other entity components: Translation and Faction
//                         // NOTE: Entities.ForEach can read components of arbitrary entity if they are present in the same query.
//                         // Here we rely on presence of Translation/Faction/AliveTag on "units" inserted to map.
//                         // However, we need to fetch component data for "other". Use EntityManager read (OK inside main thread,
//                         // but we are parallel worker here. So instead, we capture required per-entity info into the map instead of entity.
//                         // To keep code simple here, assume all units are static-ish and we can check distance by computing
//                         // distance using owner's position and scanning the cell bounding box (approx). But better approach below.
//                         // ----
//                         // For safe accurate distance we should have stored positions in the map too. See optimization notes.
//
//                         // Quick coarse test: append candidate; later consumer can filter more strictly.
//                         // compute squared distance roughly via owner pos vs other's approximate pos not accessible -> skip
//                         // To keep correctness, we will optimistically enqueue and let consumer compute actual distance using EntityManager.
//                         AppendOp op = new AppendOp { owner = owner, target = other, sqrDist = 0f };
//                         appendQueueWriter.Enqueue(op);
//                     } while (cellMap.TryGetNextValue(out other, ref it));
//                 }
//             }
//         })
//         .Schedule(buildJob); // ensure after grid is built
//
//     // ensure both jobs complete
//     JobHandle combined = JobHandle.CombineDependencies(buildJob, detectJob);
//     combined.Complete();
//
//     // 4) Single-thread: consume appendQueue and perform accurate filtering + write buffers + remove NeedTarget
//     var bufferFromEntity = GetBufferFromEntity<InsightTarget>();
//     var em = EntityManager;
//     var ecb = new EntityCommandBuffer(Allocator.Temp);
//
//     while (appendQueue.TryDequeue(out AppendOp op))
//     {
//         if (!em.Exists(op.owner) || !em.Exists(op.target)) continue;
//         // quick alive/faction/distance checks using EntityManager (single-threaded safe)
//         if (!em.HasComponent<AliveTag>(op.target)) continue;
//
//         var targetPos = em.GetComponentData<Translation>(op.target).Value;
//         var ownerPos = em.GetComponentData<Translation>(op.owner).Value;
//
//         float sqr = math.distancesq(ownerPos, targetPos);
//
//         var sight = em.GetComponentData<SightRangeSq>(op.owner).value;
//         float rangeSqr = sight * sight;
//         if (sqr > rangeSqr) continue;
//
//         int ownerFaction = em.GetComponentData<Faction>(op.owner).value;
//         int targetFaction = em.GetComponentData<Faction>(op.target).value;
//         if (ownerFaction == targetFaction) continue; // same faction -> skip
//
//         // append to DynamicBuffer
//         if (!bufferFromEntity.HasComponent(op.owner))
//         {
//             // ensure buffer exists - add an empty buffer if missing
//             ecb.AddBuffer<InsightTarget>(op.owner);
//         }
//
//         var
//             buf = bufferFromEntity[
//                 op.owner]; // safe to index after ECB changes? If we just added, BufferFromEntity won't reflect until structural change applied.
//         // To be safe: we'll use EntityManager for buffer operations if buffer exists; else apply ECB then later second pass.
//         // Simpler: if buffer missing, add and then get with EntityManager immediately:
//         if (!em.HasComponent<DynamicBuffer<InsightTarget>>(op.owner))
//         {
//             em.AddBuffer<InsightTarget>(op.owner);
//         }
//
//         var buffer = em.GetBuffer<InsightTarget>(op.owner);
//         InsightTarget it = new InsightTarget { target = op.target, sqrDistance = sqr };
//         buffer.Add(it);
//     }
//
//     // 5) Remove NeedTarget from everyone (we can batch remove)
//     // Find all entities with NeedTarget and remove. Use query to avoid per-entity ECB cost.
//     var removeQuery = GetEntityQuery(new EntityQueryDesc
//     {
//         All = new ComponentType[] { typeof(NeedTarget) }
//     });
//
//     using (var needTargets = removeQuery.ToEntityArray(Allocator.TempJob))
//     {
//         foreach (var ent in needTargets)
//         {
//             if (em.HasComponent<NeedTarget>(ent))
//                 em.RemoveComponent<NeedTarget>(ent);
//         }
//     }
//
//     // Playback ecb
//     ecb.Playback(em);
//     ecb.Dispose();
//
//     // dispose natives
//     cellMap.Dispose();
//     appendQueue.Dispose();
//
//     // set final dependency to completed (we already completed earlier)
//     Dependency = default;
// }
//
// }
// }
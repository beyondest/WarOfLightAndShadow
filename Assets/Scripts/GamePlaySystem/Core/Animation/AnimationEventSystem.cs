using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;



namespace SparFlame.GamePlaySystem.Animation
{

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class AnimationEventHandlerSystem : SystemBase
    {
        private EntityQuery _eventQuery;
        private Dictionary<int, EventHandlerDelegate> _eventDict;

        protected override void OnCreate()
        {
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            _eventDict = new Dictionary<int, EventHandlerDelegate>(10);

        }

        protected override void OnStartRunning()
        {
            _eventDict.Add(Hash("LogTest"), TestPrint);
        }

        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .WithoutBurst()
                .ForEach((Entity entity, ref DynamicBuffer<AnimationEventRequest> events) =>
                {
                    if (events.Length == 0) return;
                    foreach (var evt in events)
                    {
                        if (_eventDict.TryGetValue(evt.NameHash, out var handler))
                        {
                            handler.Invoke(entity, evt.Parameter, ecb);
                        }
                    }

                    events.Clear();
                }).Run();

            ecb.Playback(EntityManager);
            ecb.Dispose();

        }


        private delegate void EventHandlerDelegate(Entity entity, int parameter, EntityCommandBuffer ecb);


        private void TestPrint(Entity entity, int value, EntityCommandBuffer ecb)
        {
            Debug.Log($"Int : {value}");
        }

        private static int Hash(string str)
        {
            FixedString64Bytes string64 = str;
            return string64.GetHashCode();
        }
    }
}


// Cannot work in IjobEntity, maybe the dictionary managed type
    
// public partial struct CheckAnimationEvent : IJobEntity
// {
//     [ReadOnly] public Dictionary<int, EventHandlerDelegate> EventDict;
//     public EntityCommandBuffer.ParallelWriter ECB;
//     private void Execute([ChunkIndexInQuery] int index, ref DynamicBuffer<AnimationEventRequest> events, Entity entity)
//     {
//         if(events.Length == 0)return;
//         foreach (var evt in events)
//         {
//             
//                 // handler.Invoke(entity, evt.Parameter, ECB);
//             Debug.Log($"NameHash : {evt.NameHash}, value : {evt.Parameter}");
//         }
//         ECB.SetBuffer<AnimationEventRequest>(index, entity);
//
//     }
// }
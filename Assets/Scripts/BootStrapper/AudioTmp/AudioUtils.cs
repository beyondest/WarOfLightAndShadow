using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.BootStrapper
{
    public struct AudioUtils
    {
        public static void PlayAudioClip(AudioName audioName, float3 position,
            EntityCommandBuffer.ParallelWriter ecbP, int index)
        {
            var request = ecbP.CreateEntity(index);
            ecbP.AddComponent<GameplayEntityTag>(index, request);
            ecbP.AddComponent(index, request, new AudioRequest
            {
                Name = audioName,
                Position = position,
            });
        }
        public static void PlayAudioClip(AudioName audioName, float3 position,
            EntityCommandBuffer ecb)
        {
            var request = ecb.CreateEntity();
            ecb.AddComponent<GameplayEntityTag>(request);
            ecb.AddComponent(request, new AudioRequest
            {
                Name = audioName,
                Position = position,
            });
        }
        public static void PlayAudioClip(AudioName audioName, float3 position, EntityManager manager)
        {
            var request = manager.CreateEntity();
            manager.AddComponent<GameplayEntityTag>(request);
            manager.AddComponentData(request, new AudioRequest
            {
                Name = audioName,
                Position = position,
            });
        }
    }
}
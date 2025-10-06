using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Audio
{
    public struct AudioUtils
    {
        public static void PlayAudioClip(AudioName audioName, float3 position,
            EntityCommandBuffer.ParallelWriter ecbP, int index)
        {
            var request = ecbP.CreateEntity(index);
            ecbP.AddComponent<SubGameplayEntityTag>(index, request);
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
            ecb.AddComponent<SubGameplayEntityTag>(request);
            ecb.AddComponent(request, new AudioRequest
            {
                Name = audioName,
                Position = position,
            });
        }
        public static void PlayAudioClip(AudioName audioName, float3 position, EntityManager manager)
        {
            var request = manager.CreateEntity();
            manager.AddComponent<SubGameplayEntityTag>(request);
            manager.AddComponentData(request, new AudioRequest
            {
                Name = audioName,
                Position = position,
            });
        }
        
        
    }
    

    public static class CustomAudio
    {
        public static AudioSource PlayClipAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float minDistance = 1f, float maxDistance = 500f)
        {
            // 1. Create a temporary GameObject to hold the AudioSource
            GameObject tempAudioGameObject = new GameObject("TempAudio");
            tempAudioGameObject.transform.position = position;

            // 2. Add an AudioSource component and configure it
            AudioSource audioSource = tempAudioGameObject.AddComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.volume = volume;
        
            // 3. Set the custom 3D sound settings
            audioSource.spatialBlend = 1f; // Ensure it's a 3D sound
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;

            // 4. Play the clip
            audioSource.Play();

            // 5. Destroy the GameObject once the clip is finished
            Object.Destroy(tempAudioGameObject, clip.length);

            // Optional: Return a reference in case you need it for more modifications
            return audioSource;
        }
     
    }

}
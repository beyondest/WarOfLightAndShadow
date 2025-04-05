using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace DefaultNamespace
{
    public class ttt : MonoBehaviour
    {
        public float3 newSize;
        public GameObject ghost;
        
        
        public AssetReference asset;

      
        // private class GhostAttributeAuthoringBaker : Baker<ttt>
        // {
        //     public override void Bake(ttt authoring)
        //     {
        //         var entity = GetEntity(TransformUsageFlags.Dynamic);
        //         AddComponent(entity, new GhostRequest
        //         {
        //             Ghost = GetEntity(authoring.ghost, TransformUsageFlags.Dynamic),
        //             NewSize = authoring.newSize
        //         });
        //     }
        // }
    }
    public struct GhostRequest : IComponentData
    {
        public Entity Ghost;
        public float3 NewSize;
    }
}
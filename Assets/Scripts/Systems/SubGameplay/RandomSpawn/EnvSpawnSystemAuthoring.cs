using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.RandomSpawn
{
    
    
    public class EnvSpawnSystemAuthoring : MonoBehaviour
    {
        public EnvSpawnSystemConfig config;

        private class EnvSpawnSystemAuthoringBaker : Baker<EnvSpawnSystemAuthoring>
        {
            public override void Bake(EnvSpawnSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }


    [Serializable]
    public struct EnvSpawnSystemConfig : IComponentData
    {
    }




}

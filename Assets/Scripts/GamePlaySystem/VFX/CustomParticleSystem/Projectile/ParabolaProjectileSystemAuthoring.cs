using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.CustomParticleSystem
{
    public class ParabolaProjectileSystemAuthoring : MonoBehaviour
    {
        public float reachDis;
        public float heightIncreasePerUnitDis;
        private class ParabolaProjectileSystemAuthoringBaker : Baker<ParabolaProjectileSystemAuthoring>
        {
            public override void Bake(ParabolaProjectileSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ParabolaProjectileConfig
                {
                    ReachDis = authoring.reachDis,
                    HeightIncreasePerUnitDis = authoring.heightIncreasePerUnitDis
                });
            }
        }
        
    }

    public struct ParabolaProjectileConfig : IComponentData
    {
        public float ReachDis;
        public float HeightIncreasePerUnitDis;
    }
}
using System;
using SparFlame.Systems.General;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class StatSystemAuthoring : MonoBehaviour
    {
        [Header("Random seed for reassign resource stat")]
        public uint seed = 1;

        [Header("Hp regeneration/percent per hour")]
        public HpRegenerationConfig config;
            private class Baker : Baker<StatSystemAuthoring>
        {
            public override void Bake(StatSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponent(entity, new StatRnd
                {
                    Rnd = new Random(authoring.seed)
                });
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct HpRegenerationConfig : IComponentData
    {
        public float unitPercentPerHour;
        public float buildingPercentPerHour;
    }

    public struct StatRnd : IComponentData
    {
        public Random Rnd;
    }
}
using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class DamageReduceShieldBuffAuthoring : MonoBehaviour
    {
        public DamageReduceShieldBuffConfig config;
        private class CavalryMoveBuffBaker : Baker<DamageReduceShieldBuffAuthoring>
        {
            public override void Bake(DamageReduceShieldBuffAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, authoring.config);
            }
        }
    }



    
}
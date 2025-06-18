using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class CavalryMoveBuffSystemAuthoring : MonoBehaviour
    {
        public CavalryMoveBuffConfig config;
        private class CavalryMoveBuffBaker : Baker<CavalryMoveBuffSystemAuthoring>
        {
            public override void Bake(CavalryMoveBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, authoring.config);
            }
        }
    }

    [Serializable]
    public struct CavalryMoveBuffConfig : IComponentData
    {
        public float damageReduceScale;
    }

    
}
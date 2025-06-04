using System;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class LightArcherBuffSystemAuthoring : MonoBehaviour
    {
        private class LightArcherBuffSystemAuthoringBaker : Baker<LightArcherBuffSystemAuthoring>
        {
            public override void Bake(LightArcherBuffSystemAuthoring authoring)
            {
            }
        }
    }

    [Serializable]
    public struct LightArcherBuffConfig : IComponentData
    {
        public float bonusDamage;
    }
    
}
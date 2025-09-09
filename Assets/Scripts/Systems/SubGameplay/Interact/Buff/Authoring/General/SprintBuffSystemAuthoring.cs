using System;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Interact
{
    public class SprintBuffSystemAuthoring : MonoBehaviour
    {
        public SprintBuffConfig config;
        private class SprintBuffSystemAuthoringBaker : Baker<SprintBuffSystemAuthoring>
        {
            public override void Bake(SprintBuffSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }


}
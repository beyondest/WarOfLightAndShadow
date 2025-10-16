using SparFlame.Components.SubGameplay;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.Formation
{
    public class FormationConfigAuthoring : MonoBehaviour
    {
        public FormationConfig config;
        private class FormationConfigBaker : Baker<FormationConfigAuthoring>
        {
            public override void Bake(FormationConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, authoring.config);
            }
        }
    }
}
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Building
{
    /// <summary>
    /// One walkable plane is one constructable area, the walkable plane size determines the grid size
    /// </summary>
    public class ConstructableAttributeAuthoring : MonoBehaviour
    {
        private class ConstructableAttributeAuthoringBaker : Baker<ConstructableAttributeAuthoring>
        {
            public override void Bake(ConstructableAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Constructable
                {
                    Faction = FactionTag.Neutral
                });
            }
        }
    }

    public struct Constructable : IComponentData
    {
        public FactionTag Faction;
    }
}
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
        public FactionTag factionTag;
        private class ConstructableAttributeAuthoringBaker : Baker<ConstructableAttributeAuthoring>
        {
            public override void Bake(ConstructableAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Constructable
                {
                    Faction = authoring.factionTag,
                });
            }
        }
    }

    public struct Constructable : IComponentData
    {
        public FactionTag Faction;
    }
}
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Resource
{
    /// <summary>
    /// One walkable plane is one constructable area, the walkable plane size determines the grid size
    /// </summary>
    public class OccupiedAttributeAuthoring : MonoBehaviour
    {
        public FactionTag factionTag;
        private class ConstructableAttributeAuthoringBaker : Baker<OccupiedAttributeAuthoring>
        {
            public override void Bake(OccupiedAttributeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new OccupiedTag
                {
                    Faction = authoring.factionTag,
                });
            }
        }
    }

    public struct OccupiedTag : IComponentData
    {
        public FactionTag Faction;
    }
    public struct ChangeOccupiedTagRequest : IComponentData
    {
        public float3 DestroyedCrystalPos;
        public FactionTag CrystalFaction;
        public bool IsDestroyed;
    }
    
}
using Unity.Entities;
using UnityEngine;

namespace GamePlaySystem.Functionality.MainGameplay.War
{
    public class WaySystemAuthoring : MonoBehaviour
    {
        private class WaySystemAuthoringBaker : Baker<WaySystemAuthoring>
        {
            public override void Bake(WaySystemAuthoring authoring)
            {
            }
        }
    }

    public enum WarType
    {
        Encounter,
        Siege,
    }
    
    public struct WarTriggerRequest : IComponentData
    {
        public Entity Attacker;
        public Entity Defender;
        public WarType Type;
    }
}
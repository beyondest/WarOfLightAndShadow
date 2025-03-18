using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.State
{
    public class UnitStateSystemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<UnitStateSystemAuthoring>
        {
            public override void Bake(UnitStateSystemAuthoring authoring)
            {
            }
        }
    }

    public enum UnitState
    {
        Idle = 0,
        Attacking = 1,
        Moving = 2,
        Garrison = 3,
        Harvesting =4,
        Healing = 5,
    }
    
    public struct UnitStateConfig : IComponentData
    {
        
    }
    
}
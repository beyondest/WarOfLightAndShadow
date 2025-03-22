using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class StatSystemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<StatSystemAuthoring>
        {
            public override void Bake(StatSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
            }
        }
    }


    public struct StatValueCalculationConfig : IComponentData
    {
        public float Multiplier;
    }
    
    /// <summary>
    /// This request is handled by stat system,TODO pop number system
    /// </summary>
    public struct StatChangeRequest : IComponentData
    {
        public Entity Interactor;
        public Entity Interactee;
        public int Amount;
        public InteractType InteractType;
    }
    
    

}
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class StatAuthoring : MonoBehaviour
    {
        private class Baker : Baker<StatAuthoring>
        {
            public override void Bake(StatAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new StatData
                {
                    
                });
            }
        }
    }

    public struct StatData : IComponentData
    {
        public int MaxValue;
        public int CurValue;
        
    }
    


}
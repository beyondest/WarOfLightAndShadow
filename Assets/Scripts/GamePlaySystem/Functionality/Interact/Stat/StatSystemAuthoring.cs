using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;
namespace SparFlame.GamePlaySystem.Interact
{
    public class StatSystemAuthoring : MonoBehaviour
    {
        
        
        [Header("Stat Calculation Config")]
        public float statMultiplier = 1.0f;
        
        [Header("PopNumber VFX")]
        public float popNumberScale = 1.0f;
        
        [Header("Random seed for reassign resource stat")]
        public uint seed = 1;
        private class Baker : Baker<StatSystemAuthoring
         
        >
        {
            public override void Bake(StatSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new StatSystemConfig
                {
                    Multiplier   = authoring.statMultiplier,
                    PopNumberScale = authoring.popNumberScale,
                });
                AddComponent(entity, new StatRnd
                {
                    Rnd = new Random(authoring.seed)
                });
            }
        }
    }

    public struct StatSystemConfig : IComponentData
    {
        public float Multiplier;
        public float PopNumberScale;
    }

    public struct StatRnd : IComponentData
    {
        public Random Rnd;
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
using SparFlame.GamePlaySystem.General;
using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;
namespace SparFlame.GamePlaySystem.Interact
{
    public class StatSystemAuthoring : MonoBehaviour
    {
        
        

        [Header("Random seed for reassign resource stat")]
        public uint seed = 1;
        
        [Header("Hp regeneration/amount per second")]
        public float normalHpRegenerationRate;
        public float garrisonHpRegenerationRate;
        public float aiBoostRate;
        public float buildingHpRegenerationRate;
        private class Baker : Baker<StatSystemAuthoring>
        {
            public override void Bake(StatSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new StatSystemConfig
                {
                    NormalHpRegenerationRate = authoring.normalHpRegenerationRate,
                    GarrisonHpRegenerationRate = authoring.garrisonHpRegenerationRate,
                    AiBoostRate = authoring.aiBoostRate,
                    BuildingHpRegenerationRate = authoring.buildingHpRegenerationRate,
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
        public float NormalHpRegenerationRate;
        public float GarrisonHpRegenerationRate;
        public float AiBoostRate;
        public float BuildingHpRegenerationRate;
    }

    public struct StatRnd : IComponentData
    {
        public Random Rnd;
    }
    
    /// <summary>
    /// This request is handled by stat system,TODO pop number system
    /// AbsAmount is always positive
    /// </summary>
    public struct StatChangeRequest : IComponentData
    {
        public Entity Interactor;
        public Entity Interactee;
        public int AbsAmount;
        public InteractType InteractType;
        public bool KillByUnNormal;
    }
    
    
    

}
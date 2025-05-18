using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    public class ExpSystemAuthoring : MonoBehaviour
    {
        class Baker : Baker<ExpSystemAuthoring>
        {
            public override void Bake(ExpSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<ExpSystemConfig>(entity);
            }
        }
    }

    public struct ExpSystemConfig : IComponentData
    {
        
    }

    
    /// <summary>
    /// Each time the exp cur value >= max value && curTier != maxTier, add this request;
    /// And we do not upgrade by change the attributes, we upgrade by instantiate a new object at same place
    /// </summary>
    public struct LevelUpRequest : IComponentData
    {
        
    }

    public struct UpGradableTag : IComponentData
    {
        
    }
   
}

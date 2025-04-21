using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Exp
{
    public class ExpSystemAuthoring : MonoBehaviour
    {
        class Baker : Baker<ExpSystemAuthoring>
        {
            public override void Bake(ExpSystemAuthoring authoring)
            {
        
            }
        }
    }

    public enum Tier
    {
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        // Tier4 = 6,
        // Tier5 = 7,
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

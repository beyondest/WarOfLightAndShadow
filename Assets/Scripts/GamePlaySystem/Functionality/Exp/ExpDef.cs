using Unity.Entities;

namespace SparFlame.GamePlaySystem.Exp
{
    // public class ExpDef : MonoBehaviour
    // {
    //     public Tier currentTier;
    //     public int maxValue;        
    //     private class ExpAttributeAuthoringBaker : Baker<ExpDef>
    //     {
    //         public override void Bake(ExpDef authoring)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent(entity,new ExpData
    //             {
    //                 CurTier = authoring.currentTier,
    //                 MaxValue = authoring.maxValue,
    //                 CurValue = 0
    //             });
    //             
    //         }
    //     }
    //    
    // }


    
    
    public struct ExpData : IComponentData
    {
        public Tier MaxTier;
        public Tier CurTier;
        public int MaxValue;
        public float CurValue;
        public Entity NextTierPrefab;
    }

    

   
    

}
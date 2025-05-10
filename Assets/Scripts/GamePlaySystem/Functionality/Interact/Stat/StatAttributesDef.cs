using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Interact
{
    // public class StatAttributesDef : MonoBehaviour
    // {
    //     public int statMaxValue = 100;
    //     private class Baker : Baker<StatAttributesDef>
    //     {
    //         public override void Bake(StatAttributesDef attributesDef)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent(entity, new StatData
    //             {
    //                 MaxValue = attributesDef.statMaxValue,
    //                 CurValue = attributesDef.statMaxValue
    //             });
    //         }
    //     }
    // }

    public struct StatData : IComponentData
    {
        public int MaxValue;
        public float CurValue;
        
    }
        


}
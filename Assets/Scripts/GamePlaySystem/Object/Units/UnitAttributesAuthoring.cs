using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using BoxCollider = UnityEngine.BoxCollider;

namespace SparFlame.GamePlaySystem.Units
{
    public class UnitAttributesAuthoring : MonoBehaviour
    {
        
     

     
        class UnitAttributesAuthoringBaker : Baker<UnitAttributesAuthoring>
        {
            public override void Bake(UnitAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var boxCollider = authoring.GetComponent<BoxCollider>();
                AddComponent(entity, new UnitBasicAttr
                {
                });
                
            }
        }
    }

    public struct UnitBasicAttr : IComponentData
    {

    }





    
}

using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using BoxCollider = UnityEngine.BoxCollider;

namespace SparFlame.GamePlaySystem.Units
{
    public class UnitAttributesAuthoring : MonoBehaviour
    {
        
        public UnitType unitType;

        class UnitAttributesAuthoringBaker : Baker<UnitAttributesAuthoring>
        {
            public override void Bake(UnitAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new UnitAttr
                {
                    Type = authoring.unitType,
                });
                
            }
        }
    }
    public struct UnitAttr : IComponentData
    {
        public UnitType Type;
    }
    public enum UnitType
    {
        Shield = 0, // Attack
        Ranged = 1,// Attack
        Magic = 2, // Attack, heal
        Cavalry = 3, // Attack
        Worker = 4 // Attack, harvest
    }

    


    
}

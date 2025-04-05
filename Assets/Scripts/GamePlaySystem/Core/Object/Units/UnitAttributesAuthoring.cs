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
        Melee, // Attack
        Archer,// Attack
        Mage, // Attack, heal
        Cavalry, // Attack
        Farmer // Attack, harvest
    }

    


    
}

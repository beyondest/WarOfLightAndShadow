using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using BoxCollider = UnityEngine.BoxCollider;

namespace SparFlame.GamePlaySystem.Units
{
    public class UnitAttributesAuthoring : MonoBehaviour
    {
        
        [Header("General")]
        
        [Tooltip("Notice : attackRange not only influence attack abilities, but also influence the march positioning" +
                 "So even if a unit can only heal, its attackRange should be set carefully so that it will not get" +
                 "too closed to enemy")]
        public float moveSpeed = 5f;

     
        class UnitAttributesAuthoringBaker : Baker<UnitAttributesAuthoring>
        {
            public override void Bake(UnitAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var boxCollider = authoring.GetComponent<BoxCollider>();
                AddComponent(entity, new UnitBasicAttr
                {
                    BoxColliderSize = boxCollider.size,
                    MoveSpeed = authoring.moveSpeed,
                });
                
            }
        }
    }

    public struct UnitBasicAttr : IComponentData
    {
        public float3 BoxColliderSize;
        public float MoveSpeed;

    }





    
}

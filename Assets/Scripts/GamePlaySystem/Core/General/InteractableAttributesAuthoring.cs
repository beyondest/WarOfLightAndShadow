using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;


namespace SparFlame.GamePlaySystem.General
{

    public class InteractableAttributesAuthoring : MonoBehaviour
    {
        public BaseTag baseTag;
        public FactionTag factionTag;



        class Baker : Baker<InteractableAttributesAuthoring>
        {
            public override void Bake(InteractableAttributesAuthoring authoring)
            {
                var entity = GetEntity(authoring.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);
                var boxCollider = authoring.GetComponent<BoxCollider>();

                AddComponent(entity, new InteractableAttr
                {
                    BaseTag = authoring.baseTag,
                    FactionTag = authoring.factionTag,
                    BoxColliderSize = boxCollider.size
                });

                
            }
        }
    }

    /// <summary>
    /// Units, Buildings, Env, Others
    /// </summary>
    public enum BaseTag
    {
        Units,
        Buildings,
        Resources
    }

    public enum FactionTag
    {
        Neutral = 0,
        Ally = 1,
        Enemy = ~1,
    }


    public struct InteractableAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;

    }
}

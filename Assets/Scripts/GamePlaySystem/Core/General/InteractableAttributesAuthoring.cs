using UnityEngine;
using Unity.Entities;
using UnityEngine.Serialization;


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

                AddComponent(entity, new InteractableAttr
                {
                    BaseTag = authoring.baseTag,
                    FactionTag = authoring.factionTag,
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
    }
}

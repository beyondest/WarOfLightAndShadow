using UnityEngine;
using Unity.Entities;
using UnityEngine.Serialization;


namespace SparFlame.GamePlaySystem.General
{

    public class BasicAttributesAuthoring : MonoBehaviour
    {
        public BaseTag baseTag;
        [FormerlySerializedAs("teamTag")] public FactionTag factionTag;



        class Baker : Baker<BasicAttributesAuthoring>
        {
            public override void Bake(BasicAttributesAuthoring authoring)
            {
                var entity = GetEntity(authoring.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);

                AddComponent(entity, new BasicAttr
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
        Walkable,
        Resources
    }

    public enum FactionTag
    {
        Ally,
        Enemy,
        Neutral
    }


    public struct BasicAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
    }
}

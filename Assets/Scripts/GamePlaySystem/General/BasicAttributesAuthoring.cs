using UnityEngine;
using Unity.Entities;


namespace SparFlame.GamePlaySystem.General
{

    public class BasicAttributesAuthoring : MonoBehaviour
    {
        public BaseTag baseTag;
        public TeamTag teamTag;



        class Baker : Baker<BasicAttributesAuthoring>
        {
            public override void Bake(BasicAttributesAuthoring authoring)
            {
                var entity = GetEntity(authoring.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);

                AddComponent(entity, new BasicAttributes
                {
                    BaseTag = authoring.baseTag,
                    TeamTag = authoring.teamTag,
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

    public enum TeamTag
    {
        Ally,
        Enemy,
        Neutral
    }


    public struct BasicAttributes : IComponentData
    {
        public BaseTag BaseTag;
        public TeamTag TeamTag;
    }
}

using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;


namespace SparFlame.GamePlaySystem.General
{

    public class InteractableAttributesAuthoring : MonoBehaviour
    {
        public BaseTag baseTag;
        public FactionTag factionTag;
        public string gameplayName;
        public Tier tier;

        class Baker : Baker<InteractableAttributesAuthoring>
        {
            public override void Bake(InteractableAttributesAuthoring authoring)
            {
                var entity = GetEntity(authoring.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);
                var physicsShapeAuthoring = authoring.GetComponent<PhysicsShapeAuthoring>();
                AddComponent(entity, new InteractableAttr
                {
                    BaseTag = authoring.baseTag,
                    FactionTag = authoring.factionTag,
                    BoxColliderSize = physicsShapeAuthoring.m_PrimitiveSize,
                    GameplayName = authoring.gameplayName,
                    Tier = authoring.tier,
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

    public enum Tier
    {
        None = 0,
        Tier1 = 3,
        Tier2 = 4,
        Tier3 = 5,
        Tier4 = 6,
        Tier5 = 7,
    }

    
    public struct InteractableAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;
        public FixedString32Bytes GameplayName;
        public Tier Tier;
    }
}

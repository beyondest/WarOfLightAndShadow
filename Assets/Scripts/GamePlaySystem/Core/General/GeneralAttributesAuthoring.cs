using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;

namespace SparFlame.GamePlaySystem.General
{

    public class GeneralAttributesAuthoring : MonoBehaviour
    {
        public BaseTag baseTag;
        public FactionTag factionTag;
        public string gameplayName;

        class Baker : Baker<GeneralAttributesAuthoring>
        {
            public override void Bake(GeneralAttributesAuthoring authoring)
            {
                
                var entity = GetEntity(authoring.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);
                var physicsShapeAuthoring = authoring.GetComponent<PhysicsShapeAuthoring>();
                AddComponent(entity, new GeneralAttr
                {
                    BaseTag = authoring.baseTag,
                    FactionTag = authoring.factionTag,
                    BoxColliderSize = physicsShapeAuthoring.m_PrimitiveSize,
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



    
    public struct GeneralAttr : IComponentData
    {
        public BaseTag BaseTag;
        public FactionTag FactionTag;
        public float3 BoxColliderSize;
        public int ID;
    }
}

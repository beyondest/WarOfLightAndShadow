using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.General
{

    // public class GeneralAttributesDef : MonoBehaviour
    // {
    //     public BaseTag baseTag;
    //     public FactionTag factionTag;
    //
    //     class Baker : Baker<GeneralAttributesDef>
    //     {
    //         public override void Bake(GeneralAttributesDef def)
    //         {
    //             
    //             var entity = GetEntity(def.baseTag == BaseTag.Units ? TransformUsageFlags.Dynamic : TransformUsageFlags.None);
    //             var physicsShapeAuthoring = def.GetComponent<PhysicsShapeAuthoring>();
    //             AddComponent(entity, new GeneralAttr
    //             {
    //                 BaseTag = def.baseTag,
    //                 FactionTag = def.factionTag,
    //                 BoxColliderSize = physicsShapeAuthoring.m_PrimitiveSize,
    //             });
    //         }
    //     }
    // }

  
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
    
    public struct AITag : IComponentData
    {
        
    }

    public struct PlayerTag : IComponentData
    {
        
    }
    // TODO : move this tag to correct place
    public struct InTeamTag : IComponentData
    {
        public Entity BelongsToTeam;
    }

    public struct GameplayEntityTag : IComponentData
    {
        
    }
    
}

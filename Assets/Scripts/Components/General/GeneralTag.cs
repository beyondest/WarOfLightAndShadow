using Unity.Entities;

namespace SparFlame.Components.General
{
    public struct AITag : IComponentData
    {
        
    }

    public struct PlayerTag : IComponentData
    {
        
    }
    
    public struct MainGamingTag : IComponentData
    {
        
    }
    // For systems that do not need game status data, require for update this one only
    public struct SubGamingTag : IComponentData
    {
       
    }
    
    public struct MainGameplayEntityTag : IComponentData
    {
        
    }
    public struct SubGameplayEntityTag : IComponentData
    {
        
    }

    public struct ReassignMainGameplayAITagRequest : IComponentData
    {
        
    }

    public struct ReassignSubGameplayAITagRequest : IComponentData
    {
        
    }
}
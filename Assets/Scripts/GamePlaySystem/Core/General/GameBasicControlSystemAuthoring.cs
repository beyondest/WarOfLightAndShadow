using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.GamePlaySystem.General
{
    public class GameBasicControlSystemAuthoring : MonoBehaviour
    {
        class Baker : Baker<GameBasicControlSystemAuthoring>
        {
            public override void Bake(GameBasicControlSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<NotPauseTag>(entity);
 
            }
        }


    }
    

    public struct NotPauseTag : IComponentData
    {
        
    }



    public struct PauseRequest : IComponentData
    {
        
    }

    public struct ResumeRequest : IComponentData
    {
        
    }
}

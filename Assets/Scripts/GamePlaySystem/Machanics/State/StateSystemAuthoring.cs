using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Interact
{
    public class StateSystemAuthoring : MonoBehaviour
    {
        private class Baker : Baker<StateSystemAuthoring>
        {
            public override void Bake(StateSystemAuthoring authoring)
            {
            }
        }
    }

    public enum StateType
    {
        InteractiveState,
        
    }
    
    public struct StateGeneralConfig : IComponentData
    {
        
    }
    
}
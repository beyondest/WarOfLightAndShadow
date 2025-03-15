using UnityEngine;
using Unity.Entities;
namespace SparFlame.GamePlaySystem.Interact
{
    public class InteractSystemAuthoring : MonoBehaviour
    {
        private class InteractSystemAuthoringBaker : Baker<InteractSystemAuthoring>
        {
            public override void Bake(InteractSystemAuthoring authoring)
            {
            }
        }
    }

    public struct InteractConfig : IComponentData
    {
        
    }
    
}
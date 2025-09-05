using SparFlame.Components.Input;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputCameraNormalModeAuthoring : MonoBehaviour
    {
        private class InputCameraNormalModeAuthoringBaker : Baker<InputCameraNormalModeAuthoring>
        {
            public override void Bake(InputCameraNormalModeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputCameraNormalData());
            }
        }
    }
    
}
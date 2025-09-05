using SparFlame.Components.Input;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputCameraFlyModeAuthoring : MonoBehaviour
    {
        private class InputCameraFlyModeAuthoringBaker : Baker<InputCameraFlyModeAuthoring>
        {
            public override void Bake(InputCameraFlyModeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputCameraFlyData>(entity);
            }
        }
    }

    
}
using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputUnitControlAuthoring : MonoBehaviour
    {
        private class InputUnitSelectionBaker : Baker<InputUnitControlAuthoring>
        {
            public override void Bake(InputUnitControlAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputUnitControlData());

            }
        }
    }


}
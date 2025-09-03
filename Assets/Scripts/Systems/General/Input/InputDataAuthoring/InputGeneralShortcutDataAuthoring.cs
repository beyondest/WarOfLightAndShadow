using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputGeneralShortcutDataAuthoring : MonoBehaviour
    {
        private class InputGeneralShortcutDataAuthoringBaker : Baker<InputGeneralShortcutDataAuthoring>
        {
            public override void Bake(InputGeneralShortcutDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputGeneralShortcutData>(entity);
            }
        }
    }
}
using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
{
    public class InputConstructDataAuthoring : MonoBehaviour
    {
        private class InputConstructDataAuthoringBaker : Baker<InputConstructDataAuthoring>
        {
            public override void Bake(InputConstructDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputConstructData>(entity);
            }
        }
    }


}
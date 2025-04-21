using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public class InputConjureDataAuthoring : MonoBehaviour
    {
        private class InputConjureDataAuthoringBaker : Baker<InputConjureDataAuthoring>
        {
            public override void Bake(InputConjureDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new InputConjureData
                {
                    
                });
            }
        }
    }

    public struct InputConjureData : IComponentData
    {
        public bool Enabled;
        public bool FullConjure;
        public int HotKeyIndex;
    }
}
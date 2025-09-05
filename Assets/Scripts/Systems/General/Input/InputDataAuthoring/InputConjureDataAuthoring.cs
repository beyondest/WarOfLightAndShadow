using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Input
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

 
}
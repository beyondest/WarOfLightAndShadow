using SparFlame.GamePlaySystem.CustomParticleSystem;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Database
{
    public class GeneralVFXAttributesAuthoring : MonoBehaviour
    {
        
        private class GeneralVFXAttributesAuthoringBaker : Baker<GeneralVFXAttributesAuthoring>
        {
            public override void Bake(GeneralVFXAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VFXRootTag>(entity);
            }
        }
    }
}
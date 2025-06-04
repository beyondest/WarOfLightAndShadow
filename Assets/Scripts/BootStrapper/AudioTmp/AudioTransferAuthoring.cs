using Unity.Entities;
using UnityEngine;

namespace SparFlame.BootStrapper
{
    public class AudioTransferAuthoring : MonoBehaviour
    {
        public float audioRadiusToCameraRig = 50f;
        private class AudioTransferAuthoringBaker : Baker<AudioTransferAuthoring>
        {
            public override void Bake(AudioTransferAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new AudioTransferConfig
                {
                     AudioRadiusSqToCameraRig= authoring.audioRadiusToCameraRig * authoring.audioRadiusToCameraRig
                });
                
                
            }
        }
    }

    public struct AudioTransferConfig : IComponentData
    {
        public float AudioRadiusSqToCameraRig;
    }
}
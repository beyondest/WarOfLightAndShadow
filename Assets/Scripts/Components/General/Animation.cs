using Latios.Kinemation;
using Unity.Entities;
namespace SparFlame.Components.General
{
    public enum UnitAnimationState
    {
        Idle = 0,
        Attack = 1,
        March = 2,
        AlertMarch = 3,
        Die = 4,
        CastSkill = 5,
    }

    public struct AnimationStateData : IComponentData
    {
        public UnitAnimationState State;
        public float PlaySpeed;
        
        public float ClipAStartTime;
        public float ClipBStartTime;
        public float ClipAWeight;
        public float ClipBWeight;

        // Blend A To B
        public int ClipAIndex;
        public int ClipBIndex;
        public bool Blending;
    }
    
    public struct ClipBlobData : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> Blob;
    }

    public struct AnimationEventData : IBufferElementData
    {
        public int NameHash;
        public int Parameter;
    }

}
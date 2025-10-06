using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.StateMachine
{
    public class AutoGiveWayConfigAuthoring : MonoBehaviour
    {
        public float moveRatio = 1f;
        public float waitSeconds = 2f;
        public bool disable;
        public bool neverGoBack = true;
        public float maxSeparationValueNotToGiveWay = 1f;
        public bool checkBackHasTarget = true;
        private class AutoGiveWayConfigAuthoringBaker : Baker<AutoGiveWayConfigAuthoring>
        {
            public override void Bake(AutoGiveWayConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new AutoGiveWayConfig
                {
                    MoveDistanceRatioOfSelfRadius = authoring.moveRatio,
                    WaitSeconds = authoring.waitSeconds,  
                    Disable = authoring.disable,
                    NeverGoBack = authoring.neverGoBack,
                    MaxSeparationValueNotToGiveWay = authoring.maxSeparationValueNotToGiveWay,
                    CheckBackHasTarget =   authoring.checkBackHasTarget
                });
            }
        }
    }
    public struct AutoGiveWayConfig : IComponentData
    {
        public bool Disable;
        public float MoveDistanceRatioOfSelfRadius;
        public float WaitSeconds;
        public bool NeverGoBack;
        public bool CheckBackHasTarget;
        public float MaxSeparationValueNotToGiveWay;
    }
    
}
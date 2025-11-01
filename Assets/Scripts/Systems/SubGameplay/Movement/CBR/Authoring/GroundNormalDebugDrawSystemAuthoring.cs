using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.CBR.Authoring
{
    public class GroundNormalDebugDrawSystemAuthoring : MonoBehaviour
    {
        public bool debugDrawEnabled;
        private class
            GroundNormalDebugDrawSystemAuthoringBaker : Baker<GroundNormalDebugDrawSystemAuthoring>
        {
            public override void Bake(GroundNormalDebugDrawSystemAuthoring authoring)
            {
                if (authoring.debugDrawEnabled)
                {
                    var entity = GetEntity(TransformUsageFlags.None);
                    AddComponent<DebugDrawNormalVector>(entity);
                }
            }
        }
    }
    internal struct DebugDrawNormalVector : IComponentData{}
}
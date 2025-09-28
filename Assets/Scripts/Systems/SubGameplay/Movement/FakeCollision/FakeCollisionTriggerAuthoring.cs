using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    public class FakeCollisionTriggerAuthoring : MonoBehaviour
    {
        private class FakeCollisionTriggerAuthoringBaker : Baker<FakeCollisionTriggerAuthoring>
        {
            public override void Bake(FakeCollisionTriggerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var box = authoring.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize;
                AddComponent(entity, new BoxColliderSize
                {
                    Box = box,
                    Radius = math.length(box.xz) / 2f
                });
            }
        }
    }
}
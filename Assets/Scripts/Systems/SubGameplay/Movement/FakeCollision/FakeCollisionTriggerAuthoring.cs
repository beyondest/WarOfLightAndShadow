using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Movement.FakeCollision
{
    public class FakeCollisionTriggerAuthoring : MonoBehaviour
    {
        public float separationBoxSizeMinus;
        private class FakeCollisionTriggerAuthoringBaker : Baker<FakeCollisionTriggerAuthoring>
        {
            public override void Bake(FakeCollisionTriggerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var box = authoring.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize;
                box.x -= authoring.separationBoxSizeMinus;
                box.y -= authoring.separationBoxSizeMinus;
                box.z -= authoring.separationBoxSizeMinus;
                AddComponent(entity, new BoxColliderSize
                {
                    SeparationBox = box,
                    Radius = math.length(box.xz) / 2f
                });
            }
        }
    }
}
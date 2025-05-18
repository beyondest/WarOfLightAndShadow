using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class BuildingPackTagAuthoring : MonoBehaviour
    {
        private class BuildingPackTagAuthoringBaker : Baker<BuildingPackTagAuthoring>
        {
            public override void Bake(BuildingPackTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var colliderBox = authoring.GetComponent<PhysicsShapeAuthoring>().m_PrimitiveSize;
                AddComponent(entity, new BuildingPackSquareSize
                {
                    Value = colliderBox.xz
                });
            }
        }
    }


    public struct BuildingPackSquareSize : IComponentData
    {
        public float2 Value;
    }
}
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Database
{
    public class GeneralEnvAttributesAuthoring : MonoBehaviour
    {

        private class Baker : Baker<GeneralEnvAttributesAuthoring>
        {
            public override void Bake(GeneralEnvAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                const float volumeRadius = 0f;
                var physicsShapeAuthoring = authoring.GetComponent<PhysicsShapeAuthoring>();
                if (physicsShapeAuthoring)
                {
                    AddComponent<VolumeObstacleTag>(entity);
                    AddComponent(entity, new VolumeObstacleSpawnRequest
                    {
                        Center = physicsShapeAuthoring.m_PrimitiveCenter,
                        Size = physicsShapeAuthoring.m_PrimitiveSize,
                        VolumeRadius = volumeRadius,
                        VolumeAreaType = AreaType.NotWalkable,
                        RequestFromFaction = FactionTag.Neutral,
                    });
                    SetComponentEnabled<VolumeObstacleSpawnRequest>(entity, true);
                }
               
            }
        }
    }
}
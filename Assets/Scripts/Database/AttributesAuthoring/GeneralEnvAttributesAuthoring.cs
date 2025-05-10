using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Fow;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Movement;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEngine;

namespace SparFlame.Database
{
    public class GeneralEnvAttributesAuthoring : MonoBehaviour
    {
        public float disappearInFowThreshold = 0.1f;
        public bool ifInverseAgent;
        public bool debugShow;
        private class Baker : Baker<GeneralEnvAttributesAuthoring>
        {
            public override void Bake(GeneralEnvAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                const float volumeRadius = 0f;
                var physicsShapeAuthoring = authoring.GetComponent<PhysicsShapeAuthoring>();
                if (physicsShapeAuthoring != null)
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
                }
                if (!authoring.debugShow)
                {
                    var fowAgentData = new FowAgentData
                    {
                        SightRange = 0,
                        SightCos = Mathf.Cos(360f * 0.5f * Mathf.Deg2Rad),
                        DisappearAlphaThreshold = authoring.disappearInFowThreshold,
                        IsInsight = false, 
                    };
                    AddComponent(entity, fowAgentData);
                    if (authoring.ifInverseAgent)
                    {
                        AddComponent<InverseDisappearTag>(entity);
                    }
                    else
                    {
                        AddComponent(entity, new HideFowAgentRequest
                        {
                            Hide = true
                        });
                    }
                }
               
            }
        }
    }
}
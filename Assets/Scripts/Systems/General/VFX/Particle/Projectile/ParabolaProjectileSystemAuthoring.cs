using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.VFX
{
    public class ParabolaProjectileSystemAuthoring : MonoBehaviour
    {
        public float reachDis;
        public float heightIncreasePerUnitDis;
        public float g = 9.81f;
        private class ParabolaProjectileSystemAuthoringBaker : Baker<ParabolaProjectileSystemAuthoring>
        {
            public override void Bake(ParabolaProjectileSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ParabolaProjectileConfig
                {
                    ReachDis = authoring.reachDis,
                    HeightIncreasePerUnitDis = authoring.heightIncreasePerUnitDis,
                    G = authoring.g
                });
            }
        }
        
    }

    public struct ParabolaProjectileConfig : IComponentData
    {
        public float ReachDis;
        public float HeightIncreasePerUnitDis;
        public float G;
    }
}
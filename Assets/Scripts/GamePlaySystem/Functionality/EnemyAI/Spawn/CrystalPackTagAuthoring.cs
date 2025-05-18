using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class CrystalPackTagAuthoring : MonoBehaviour
    {
        private class CrystalPackTagAuthoringBaker : Baker<CrystalPackTagAuthoring>
        {
            public override void Bake(CrystalPackTagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CrystalPackNeedInitTag>(entity);
            }
        }
    }
    public struct CrystalPackNeedInitTag : IComponentData
    {
        
    }
}
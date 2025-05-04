using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.General
{
    public class CoreCrystalAuthoring : MonoBehaviour
    {
        public FactionTag allyOrEnemy;
        private class CoreCrystalBaker : Baker<CoreCrystalAuthoring>
        {
            public override void Bake(CoreCrystalAuthoring authoring)
            {
                // var entity = GetEntity(TransformUsageFlags.Dynamic);
                // if(authoring.allyOrEnemy == FactionTag.Ally)
                //     AddComponent<AllyCoreCrystalTag>(entity);
                // else if (authoring.allyOrEnemy == FactionTag.Enemy)
                //     AddComponent<EnemyCoreCrystalTag>(entity);
            }
        }
    }

    public struct CoreCrystalTag : IComponentData
    {
        public FactionTag Faction;
    }
    
    // public struct AllyCoreCrystalTag : IComponentData
    // {
    //     
    // }
    //
    // public struct EnemyCoreCrystalTag : IComponentData
    // {
    //     
    // }
}
using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.Battle
{
    public class BattleTriggerSystemAuthoring : MonoBehaviour
    {

        [AssetsOnly] public GameObject battleCheckSightPrefab;
        
        private class BattleTriggerSystemAuthoringBaker : Baker<BattleTriggerSystemAuthoring>
        {
            
            
            public override void Bake(BattleTriggerSystemAuthoring authoring)
            {

                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new BattleTriggerConfig
                {
                    BattleCheckSightPrefab = GetEntity(authoring.battleCheckSightPrefab,TransformUsageFlags.Dynamic)
                });
            }
        }
        
    }

    public struct BattleTriggerConfig : IComponentData
    {
        public Entity BattleCheckSightPrefab;
    }
}
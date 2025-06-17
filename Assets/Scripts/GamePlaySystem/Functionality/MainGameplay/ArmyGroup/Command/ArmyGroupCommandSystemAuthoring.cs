using Sirenix.OdinInspector;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Functionality.MainGameplay.ArmyGroup
{
    public class ArmyGroupCommandSystemAuthoring : MonoBehaviour
    {
        [AssetsOnly]
        public GameObject flagPrefab;
        private class ArmyGroupCommandSystemBaker : Baker<ArmyGroupCommandSystemAuthoring>
        {
            public override void Bake(ArmyGroupCommandSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ArmyGroupCommandSystemConfig
                {
                    FlagPrefab = GetEntity(authoring.flagPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

    public struct ArmyGroupCommandSystemConfig : IComponentData
    {
        public Entity FlagPrefab;
    }
}
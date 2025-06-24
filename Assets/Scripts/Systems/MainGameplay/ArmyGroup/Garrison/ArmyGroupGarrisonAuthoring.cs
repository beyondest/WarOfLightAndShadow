using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.MainGameplay.ArmyGroup
{
    public class ArmyGroupGarrisonAuthoring : MonoBehaviour
    {
        private class ArmyGroupGarrisonAuthoringBaker : Baker<ArmyGroupGarrisonAuthoring>
        {
            public override void Bake(ArmyGroupGarrisonAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity,new ArmyGroupGarrisonSystemConfig
                {
                    
                });
            }
        }
    }

    public struct ArmyGroupGarrisonSystemConfig : IComponentData
    {
        
    }

   
}
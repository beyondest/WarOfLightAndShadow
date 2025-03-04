using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using UnityEngine;
namespace SparFlame.System.General
{

    [DisallowMultipleComponent]
    public class li : LinkedEntityGroupAuthoring
    {
        private class Baker : Baker<li>
        {
            public override void Bake(li authoring)
            {
                var parentEntity = GetEntity(TransformUsageFlags.Dynamic);
                var linkedEntities = AddBuffer<LinkedEntityGroup>(parentEntity);
                
                
                linkedEntities.Add(new LinkedEntityGroup { Value = parentEntity });

                foreach (Transform child in authoring.transform)
                {
                    var childEntity = GetEntity(child, TransformUsageFlags.Dynamic);

                    if (childEntity != Entity.Null)
                    {
                        linkedEntities.Add(new LinkedEntityGroup { Value = childEntity });
                    }
                }
            }
        }
    }



}
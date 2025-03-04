using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.System.UnitSelection
{

    public class UnitSelectionAttributesAuthoring : MonoBehaviour
    {
        class Baker : Baker<UnitSelectionAttributesAuthoring>
        {
            public override void Bake(UnitSelectionAttributesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Selected>(entity);
                SetComponentEnabled<Selected>(entity, false);
                AddComponent<LockSelectedWorkForDrag>(entity);
                SetComponentEnabled<LockSelectedWorkForDrag>(entity, false);
                AddComponent(entity, new ScreenPos
                {
                    ScreenPosition = float2.zero
                });
                //AddComponent<Selectable>(entity);
            }
        }
    }

    public struct Selected : IComponentData, IEnableableComponent
    {
    }

    public struct LockSelectedWorkForDrag : IComponentData, IEnableableComponent
    {
        
    }

    public struct ScreenPos : IComponentData
    {
        public float2 ScreenPosition;
    }
    //public struct Selectable : IComponentData
    //{

    //}
}

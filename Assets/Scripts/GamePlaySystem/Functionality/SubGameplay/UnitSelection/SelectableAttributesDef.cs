using Unity.Entities;

namespace SparFlame.GamePlaySystem.UnitSelection
{

    // public class UnitSelectionAttributesAuthoring : MonoBehaviour
    // {
    //     class Baker : Baker<UnitSelectionAttributesAuthoring>
    //     {
    //         public override void Bake(UnitSelectionAttributesAuthoring authoring)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent<Selected>(entity);
    //             SetComponentEnabled<Selected>(entity, false);
    //             AddComponent<LockSelectedWorkForDrag>(entity);
    //             SetComponentEnabled<LockSelectedWorkForDrag>(entity, false);
    //             AddComponent(entity, new ScreenPos
    //             {
    //                 ScreenPosition = float2.zero
    //             });
    //         }
    //     }
    // }

    public struct Selected : IComponentData, IEnableableComponent
    {
    }

    public struct LockSelectedWorkForDrag : IComponentData, IEnableableComponent
    {
        
    }




  
}

using Unity.Entities;

namespace SparFlame.GamePlaySystem.State
{
    // public class BasicStateAttributesAuthoring : MonoBehaviour
    // {
    //     private class StateAttributesAuthoringBaker : Baker<BasicStateAttributesAuthoring>
    //     {
    //         public override void Bake(BasicStateAttributesAuthoring authoring)
    //         {
    //             var entity = GetEntity(TransformUsageFlags.Dynamic);
    //             AddComponent(entity, new BasicStateData
    //             {
    //                 CurState = InteractState.Idle,
    //                 Focus = false,
    //                 TargetEntity = Entity.Null,
    //                 TargetState = InteractState.Idle,
    //                 InteractCounter = 0
    //             });
    //             AddComponent<IdleStateTag>(entity);
    //             SetComponentEnabled<IdleStateTag>(entity,true);
    //         }
    //     }
    // }
    
    public enum InteractState
    {
        Idle = 0,
        Attacking = 1,
        Moving = 2,
        Garrison = 3,
        Harvesting =4,
        Healing = 5,
    }

    public struct BasicStateData : IComponentData
    {
        public InteractState CurState;
        public bool Focus;
        public Entity TargetEntity;
        public InteractState TargetState;
        public int InteractCounter;
    }
    



    
    
}
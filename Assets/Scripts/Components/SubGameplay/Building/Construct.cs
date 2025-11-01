using SparFlame.Components.General;
using Unity.Entities;
using Unity.Transforms;

namespace SparFlame.Components.SubGameplay
{
    public enum PlacementStateType
    {
        Valid,
        Overlapping,
        NotEnoughResources,
        NotConstructable,
    }
    public enum ConstructCommandType
    {
        None = 0,
        Drag = 1,
        Start = 2,
        End = 3,
        Build = 4
    }

    public struct ConstructCommandData : IComponentData
    {
        // Command side
        public LocalTransform OriTransform;
        public Entity TargetBuilding;
        public Entity GhostModelEntity; // Only the model of target building
        public Entity GhostTriggerEntity;
        public Entity PreviewAttackRangeEntity;
        public Entity PreviewCube;

        public float RotationAngle;
        public ConstructCommandType CommandType;
        public PlacementStateType State;

        public bool IsMovementShow;
        public bool EnterConstruct;
    }

    public struct ConstructableTag : IComponentData,IEnableableComponent
    {
        
    }
}
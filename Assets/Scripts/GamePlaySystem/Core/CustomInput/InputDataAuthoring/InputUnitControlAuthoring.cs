using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public class InputUnitControlAuthoring : MonoBehaviour
    {
        private class InputUnitSelectionBaker : Baker<InputUnitControlAuthoring>
        {
            public override void Bake(InputUnitControlAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new InputUnitControlData());

            }
        }
    }
    public struct InputUnitControlData : IComponentData
    {
        public bool Enabled;
        public bool SingleSelect;
        public bool DragSelectStart;
        public bool DraggingSelect;
        public bool DragSelectEnd;
        public bool AddUnit;
        public bool ChangeFaction;
        public bool Focus;
        public bool Command;
    }

}
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.CustomInput
{
    public class InputConstructDataAuthoring : MonoBehaviour
    {
        private class InputConstructDataAuthoringBaker : Baker<InputConstructDataAuthoring>
        {
            public override void Bake(InputConstructDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<InputConstructData>(entity);
            }
        }
    }

    public struct InputConstructData : IComponentData
    {
        public bool Enabled;
        public bool Build;
        public bool Cancel;
        public float Rotate;
        public bool LeftRotate;
        public bool RightRotate;
        public bool Snap;
        public bool FineAdjustment;
        public bool Recycle;
        public bool Store;
        public bool MoveBuilding;
        public bool Exit;   // Exit by Button
        public bool Enter;  // Enter by Button
    }
}
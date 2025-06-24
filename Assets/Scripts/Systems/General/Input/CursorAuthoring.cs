using SparFlame.Components.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.SubGameplay.Command
{
    public class CursorAuthoring : MonoBehaviour
    {
        
        private class CursorSystemAuthoringBaker : Baker<CursorAuthoring>
        {
            public override void Bake(CursorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SubGameplayCursorData
                {
                    LeftCursorType = SubGameplayCursorType.UI,
                    RightCursorType = SubGameplayCursorType.None
                });
                AddComponent(entity, new MainGameplayCursorData
                {
                    CursorType = MainGameplayCursorType.None
                });
                AddComponent<CircleCursorData>(entity);
            }
        }
    }


    
    
}
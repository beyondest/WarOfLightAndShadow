using Unity.Entities;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Command
{
    public class CursorManageSystemAuthoring : MonoBehaviour
    {
        
        private class CursorSystemAuthoringBaker : Baker<CursorManageSystemAuthoring>
        {
            public override void Bake(CursorManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CursorData
                {
                    LeftCursorType = CursorType.UI,
                    RightCursorType = CursorType.None
                });
            }
        }
    }


    public struct CursorData : IComponentData
    {
        public CursorType LeftCursorType;
        public CursorType RightCursorType;
    }

    public enum CursorType
    {
        
        // EdgeScroll Cursors
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ArrowLeftUp,
        ArrowRightUp,
        ArrowLeftDown,
        ArrowRightDown,
        
        // GamePlay Cursors
        ControlSelect,
        CheckInfo,
        Gather,
        None,
        
        // Ally units control
        Attack,
        Heal,
        March,
        Garrison,
        Harvest,

        
        // Zoom Cursors
        ZoomIn,
        ZoomOut,
        
        // Drag Cursor
        Drag,
        
        // UI 
        UI
        
    }
    
    
    
}
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.Cursor
{
    public class CursorManageSystemAuthoring : MonoBehaviour
    {
        
        private class CursorSystemAuthoringBaker : Baker<CursorManageSystemAuthoring>
        {
            public override void Bake(CursorManageSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CursorManageData
                {
                    LeftCursorType = CursorType.UI,
                    RightCursorType = CursorType.None
                });
            }
        }
    }


    public struct CursorManageData : IComponentData
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
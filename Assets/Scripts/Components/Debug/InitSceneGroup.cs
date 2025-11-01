using System;
using Unity.Entities;

namespace SparFlame.Components.General
{
    [Serializable]
    public struct EnableDebugInitSceneGroup : IComponentData
    {
        public bool value;
    }

}
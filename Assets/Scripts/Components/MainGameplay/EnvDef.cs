using System;
using UnityEngine;

namespace SparFlame.Components.MainGameplay
{
    public enum EcoType
    {
        Unknown = 0,
        EcoType1 = 1,
        EcoType2 = 2,
        
    }

    [Serializable]
    public struct EcoColorEntry
    {
        public EcoType eco;
        public Color color; // 在 Inspector 可见，也可以通过 Hex 设置
    }
}
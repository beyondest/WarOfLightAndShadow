using System;
using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.Components.MainGameplay
{
    public struct BeforeBattleSnapShot : IComponentData
    {
        public int UnitCount;
        public float MaxHp;
    }

    public struct BeforeBattleUnit : IBufferElementData
    {
        public Entity Unit;
        public Tier Tier;
        public int Level;
    }
    
}
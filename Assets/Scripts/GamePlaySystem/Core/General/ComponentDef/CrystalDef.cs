using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.GamePlaySystem.General
{


    public struct CrystalDef : IComponentData
    {
        public FactionTag Faction;
    }


    public struct LightSingleCrystalTag : IComponentData
    {
        
    }
        
    public struct EnemyCrystalInfo : IComponentData
    {
        public int TotalCount;
        /// <summary>
        /// This only counts in sight crystal, and if enemy is light, only count single crystal
        /// </summary>
        public int InSightValidCount;
        public float CurTotalHp;
        public float MaxTotalHp;
    }
    public struct PlayerCrystalInfo : IComponentData
    {
        public int TotalCount;
        public float CurTotalHp;
        public float MaxTotalHp;
    }

}
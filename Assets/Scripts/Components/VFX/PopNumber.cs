using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.VFX
{
    public enum PopNumberType
    {
        DamageTaken = 0,
        DamageDealt = 1,
        AllyHealed = 2,
        EnemyHealed = 3,
        AllyHarvest= 4,
        EnemyHarvest = 5,
        UnNormalKill = 6,
        LightGenerate = 7,
        DarkGenerate = 8,
    }
    


    
    public struct PopNumberRequest : IComponentData
    {
        public float3 Position;
        public int Value;
        public int ColorId;
    }

   
}
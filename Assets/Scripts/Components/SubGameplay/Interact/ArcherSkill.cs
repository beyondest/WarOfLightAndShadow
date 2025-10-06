using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Components.SubGameplay
{
   
   public struct PlayerArcherSkill : IComponentData
   {
      public float3 TargetPosition;
      public bool Fire;
   }

   public struct EnemyArcherSkill : IComponentData
   {
      public float3 TargetPosition;
      public bool Fire;
   }
}
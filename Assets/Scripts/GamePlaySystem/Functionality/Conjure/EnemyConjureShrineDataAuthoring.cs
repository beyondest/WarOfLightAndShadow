using Unity.Entities;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.EnemyAI
{
    public class EnemyConjureShrineDataAuthoring : MonoBehaviour
    {
        
        private class EnemyConjureShrineDataBaker : Baker<EnemyConjureShrineDataAuthoring>
        {
            public override void Bake(EnemyConjureShrineDataAuthoring authoring)
            {

            }
        }
    }

    public struct EnemyConjureShrineData : IComponentData
    {
        public Random Rnd;
        public Entity Base;
        public float ConjureTime;
    }
}
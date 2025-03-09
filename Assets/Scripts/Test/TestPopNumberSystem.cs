using SparFlame.GamePlaySystem.General;
using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;
using SparFlame.GamePlaySystem.PopNumber;

namespace SparFlame.Test
{
    public partial class TestPopNumberSystem : SystemBase
    {
        private Random _rnd;
        private int count;

        protected override void OnCreate()
        {
            _rnd = new Random(8);
            RequireForUpdate<NotPauseTag>();
            RequireForUpdate<PopNumberColorConfig>();
            RequireForUpdate<TestSpawner>();
            count = 60;
        }

        protected override void OnUpdate()
        {
            count--;
            if (count > 0) return;
            count = 60;
            var colorConfig = SystemAPI.GetSingletonBuffer<PopNumberColorConfig>();
            var spawner = SystemAPI.GetSingleton<TestSpawner>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entities = spawner.EntitiesPerFrame;
            while (entities-- > 0)
            {

                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new PopNumberRequest
                {
                    Value = _rnd.NextInt(1, 999999),
                    ColorId = 0,
                    Position = spawner.SpawnPosition,
                    Scale = spawner.InitialScale,
                    Type = PopNumberType.DamageTaken
                });
            }
            ecb.Playback(EntityManager);
        }
    }
}
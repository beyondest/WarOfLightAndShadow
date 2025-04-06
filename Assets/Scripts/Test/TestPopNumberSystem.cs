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
        private int _count;

        protected override void OnCreate()
        {
            _rnd = new Random(8);
            RequireForUpdate<TestPopNumberConfig>();
            _count = 60;
        }

        protected override void OnUpdate()
        {
            _count--;
            if (_count > 0) return;
            var config = SystemAPI.GetSingleton<TestPopNumberConfig>();
            _count = config.Count;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var entities = config.EntitiesPerFrame;
            while (entities-- > 0)
            {
                var entity = ecb.CreateEntity();
                ecb.AddComponent(entity, new PopNumberRequest
                {
                    Value = config.Value < 0 ? _rnd.NextInt(1, 999999) : config.Value,
                    ColorId = 0,
                    Position = config.SpawnPosition,
                    Scale = config.InitialScale,
                });
            }
            ecb.Playback(EntityManager);
        }
    }
}
using SparFlame.Components.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace SparFlame.Systems.Hints
{
    public partial struct HintsManageSystem : ISystem
    {
        private NativeHashMap<int, HintConfigs> _hintTypeToHintContent;
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<HintConfigs>();
            state.EntityManager.CreateSingletonBuffer<HintsInfo>();
            
        }
        

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!_hintTypeToHintContent.IsCreated)
            {
                Initialize();
            }
            var buffer = SystemAPI.GetSingletonBuffer<HintsInfo>();
            var curTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            buffer.Clear();
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var set = new NativeHashSet<int>(2,Allocator.Temp);
            foreach (var (request, entity) in SystemAPI.Query<RefRO<HintRequest>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
                if(!set.Add((int)request.ValueRO.Name))continue;
                if (math.lengthsq(request.ValueRO.Position) > 0.01f)
                {
                    continue;
                }
                buffer.Add(new HintsInfo
                {
                    Content = _hintTypeToHintContent[(int)request.ValueRO.Name].Content,
                    UpdateTime = curTime,
                    HintType = _hintTypeToHintContent[(int)request.ValueRO.Name].Type
                });
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            set.Dispose();
        }
        
        

        private void Initialize()
        {
            var buffer = SystemAPI.GetSingletonBuffer<HintConfigs>();
            
            _hintTypeToHintContent = new NativeHashMap<int, HintConfigs>(3, Allocator.Persistent);
            foreach (var config in buffer)
            {
                _hintTypeToHintContent.Add((int)config.Name, config);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if(_hintTypeToHintContent.IsCreated)
                _hintTypeToHintContent.Dispose();

        }
    }
}
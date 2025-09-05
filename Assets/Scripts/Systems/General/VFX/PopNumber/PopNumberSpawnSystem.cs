using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Components.VFX;
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace SparFlame.Systems.General.VFX
{
    public partial struct PopNumberSpawnSystem : ISystem
    {
        private NativeArray<float4> _colorConfig;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GameTimeData>();
            state.RequireForUpdate<CameraData>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<SubGamingTag>();
            state.RequireForUpdate<PopNumberConfig>();
            state.RequireForUpdate<PopNumberColorConfig>();
            state.RequireForUpdate<PopNumberRequest>();
        }

        public void OnDestroy(ref SystemState state)
        {
            _colorConfig.Dispose();
        }

        // [RequiredMember]
        // public void OnStartRunning(ref SystemState state)
        // {
        //     Debug.Log("OnStartRunning");
        // }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (_colorConfig == default)
            {
                var colorConfig = SystemAPI.GetSingletonBuffer<PopNumberColorConfig>(true);
                _colorConfig = new NativeArray<float4>(colorConfig.Length, Allocator.Persistent);
                for (var i = 0; i < _colorConfig.Length; i++) _colorConfig[i] = colorConfig[i].Color;
            }

            var config = SystemAPI.GetSingleton<PopNumberConfig>();
            var elapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var cameraData = SystemAPI.GetSingleton<CameraData>();

            new ApplyGlyphsJob
            {
                Ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                ElapsedTime = elapsedTime,
                ColorConfig = _colorConfig,
                Config = config,
                CameraData = cameraData
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ApplyGlyphsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public PopNumberConfig Config;
            [ReadOnly] public NativeArray<float4> ColorConfig;
            [ReadOnly] public CameraData CameraData;

            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
                in PopNumberRequest popNumberRequest)
            {
                var number = popNumberRequest.Value;
                var color = ColorConfig[popNumberRequest.ColorId];
                var glyphPosition = popNumberRequest.Position;
                var totalOffset = math.log10(number) / 2f * Config.GlyphWidth;
                
                var endPosition = CameraData.CameraRight * totalOffset + glyphPosition;
                // split to numbers
                // we iterate from  rightmost digit to leftmost
                while (number > 0)
                {
                    var digit = number % 10;
                    number /= 10;
                    var glyph = Ecb.Instantiate(chunkIndex, Config.GlyphPrefab);
                    Ecb.AddComponent<SubGameplayEntityTag>(chunkIndex, glyph);
                    // quaternion.LookRotationSafe(glyphPosition - CameraData.WorldPosition, math.up());
                    Ecb.SetComponent(chunkIndex, glyph, new LocalTransform
                    {
                        Position = endPosition,
                        Rotation = quaternion.identity,
                        Scale =Config.Scale
                    });

                    endPosition -= CameraData.CameraRight * Config.GlyphWidth;
                    endPosition -= Config.GlyphDeepOffset * CameraData.CameraForward;
                    Ecb.AddComponent(chunkIndex, glyph,
                        new PopNumberData
                        {
                            SpawnTime = ElapsedTime,
                            OriginalY = endPosition.y,
                            OriginalPosition = endPosition
                        });

                    Ecb.SetComponent(chunkIndex, glyph, new PopNumberIDFloatOverride { Value = digit });
                    Ecb.SetComponent(chunkIndex, glyph, new PopNumberColorVector4Override { Value = color });
                }

                Ecb.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}
using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using SparFlame.System.General;
using UnityEngine;
using UnityEngine.Scripting;

namespace SparFlame.System.PopNumber
{
    public partial struct PopNumberSpawnSystem : ISystem
    {
        private NativeArray<float4> _colorConfig;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<PopNumberConfig>();
            state.RequireForUpdate<PopNumberColorConfig>();
            state.RequireForUpdate<NotPauseTag>();
            state.RequireForUpdate<PopNumberRequest>();
        }
        
        public void OnDestroy(ref SystemState state)
        {
            _colorConfig.Dispose();
        }
        
        [RequiredMember]
        public void OnStartRunning(ref SystemState state)
        {
            Debug.Log("OnStartRunning");
        }
        
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

            var elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

            new ApplyGlyphsJob
            {
                Ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                ElapsedTime = elapsedTime,
                ColorConfig = _colorConfig,
                GlyphEntity = config.GlyphPrefab,
                GlyphZOffset = config.GlyphZOffset,
                GlyphWidth = config.GlyphWidth
            }.ScheduleParallel();
        }

        [BurstCompile]
        public partial struct ApplyGlyphsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public Entity GlyphEntity;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public float GlyphZOffset;
            [ReadOnly] public float GlyphWidth;
            [ReadOnly] public NativeArray<float4> ColorConfig;

            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
                in PopNumberRequest popNumberRequest)
            {
                var number = popNumberRequest.Value;
                var color = ColorConfig[popNumberRequest.ColorId];
                var glyphPosition = popNumberRequest.Position;
                var offset = math.log10(number) / 2f * GlyphWidth;
                glyphPosition.x += offset;


                // split to numbers
                // we iterate from  rightmost digit to leftmost
                while (number > 0)
                {
                    var digit = number % 10;
                    number /= 10;
                    var glyph = Ecb.Instantiate(chunkIndex, GlyphEntity);
                    Ecb.SetComponent(chunkIndex, glyph, new LocalTransform
                    {
                        Position = glyphPosition,
                        Rotation = quaternion.identity,
                        Scale = popNumberRequest.Scale
                    });

                    glyphPosition.x -= GlyphWidth;
                    glyphPosition.z -= GlyphZOffset;
                    Ecb.AddComponent(chunkIndex, glyph,
                        new PopNumberData
                        {
                            SpawnTime = ElapsedTime,
                            OriginalY = glyphPosition.y,
                            Type = popNumberRequest.Type,
                        });

                    Ecb.SetComponent(chunkIndex, glyph, new PopNumberIDFloatOverride { Value = digit });
                    Ecb.SetComponent(chunkIndex, glyph, new PopNumberColorVector4Override { Value = color });
                }

                Ecb.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}
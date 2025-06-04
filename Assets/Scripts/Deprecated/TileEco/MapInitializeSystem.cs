// using SparFlame.GamePlaySystem.General;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Rendering;
// using Unity.Transforms;
// using Random = Unity.Mathematics.Random;
//
// namespace SparFlame.GamePlaySystem.Map
// {
//     [UpdateInGroup(typeof(InitializationSystemGroup))]
//     [UpdateAfter(typeof(GameTimeSystem))]
//     public partial struct MapInitializeSystem : ISystem
//     {
//
//         [BurstCompile]
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate<GameTimeData>();
//             state.RequireForUpdate<TileMaterialConfig>();
//             state.RequireForUpdate<MapInitInfo>();
//             state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
//             state.RequireForUpdate<MapInfo>();
//             state.RequireForUpdate<GameStatusData>();
//         }
//
//         [BurstCompile]
//         public void OnUpdate(ref SystemState state)
//         {
//             var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
//             if (gameStatusData == GameStatus.Init)
//             {
//                 var buffer = SystemAPI.GetSingletonBuffer<MapLocTypeToTileType>();
//                 buffer.Clear();
//                 buffer.Add(new MapLocTypeToTileType { Type = TileType.CentralHollow });
//                 ref var generalRandom = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
//                 MapUtils.AddRandomizedTilesToBuffer(buffer, ref generalRandom.Rnd);
//                 buffer.Add(new MapLocTypeToTileType { Type = TileType.Rift });
//                 var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
//                 new MapInitJob
//                 {
//                     ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
//                     MapInfoData = SystemAPI.GetSingleton<MapInfo>(),
//                     MapLocTypeToTileTypes = buffer,
//                     SeedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt(),
//                     TileMaterialConfig = SystemAPI.GetSingleton<TileMaterialConfig>(),
//                     ElapsedTime = SystemAPI.GetSingleton<GameTimeData>().ElapsedTime,
//                     TileTypeToSubTexProbs = SystemAPI.GetSingletonBuffer<TileTypeToSubTexProbs>()
//                 }.ScheduleParallel();
//             }
//         }
//
//
//         [BurstCompile]
//         public partial struct MapInitJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ECB;
//             [ReadOnly] public TileMaterialConfig TileMaterialConfig;
//             [ReadOnly] public float ElapsedTime;
//             [ReadOnly] public int SeedBias;
//             [ReadOnly] public MapInfo MapInfoData;
//             [ReadOnly] public DynamicBuffer<MapLocTypeToTileType> MapLocTypeToTileTypes;
//             [ReadOnly] public DynamicBuffer<TileTypeToSubTexProbs> TileTypeToSubTexProbs;
//
//             private void Execute([EntityIndexInQuery] int index, ref TileData data, in LocalTransform transform,
//                 in DynamicBuffer<LinkedEntityGroup> children)
//             {
//                 var model = children[1].Value;
//
//                 var seed = GeneralUtils.GetSeedByIndexTimeBias(index, SeedBias, ElapsedTime);
//                 var rnd = new Random(seed);
//                 // Calculate material override properties
//                 var offset = rnd.NextFloat2(TileMaterialConfig.offsetRange.lower, TileMaterialConfig.offsetRange.upper);
//                 var rotation = rnd.NextFloat(TileMaterialConfig.rotationRange.lower,
//                     TileMaterialConfig.rotationRange.upper);
//                 float noiseWeight;
//                 float noiseScale;
//                 float aIdx;
//                 float bIdx;
//                 // Calculate the type data
//                 TileType tileTypeA;
//                 TileType tileTypeB;
//
//
//                 if (!MapUtils.TryGetMapTileTransition(transform.Position, MapInfoData.WorldCenter,
//                         MapInfoData.OuterSquareSize,
//                         MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
//                         out var locTypeA, out var locTypeB, out var weightA, out _))
//                 {
//                     // If this is not in a transition area, randomly choose a sub texture to use as noise
//                     var locType = MapUtils.GetMapLocTypeByPos(transform.Position, MapInfoData.WorldCenter,
//                         MapInfoData.OuterSquareSize, MapInfoData.InnerSquareSize, MapInfoData.CenterRadius);
//                     var mapCenterType = MapLocTypeToTileTypes[(int)locType].Type;
//                     tileTypeA = mapCenterType;
//                     tileTypeB = TileType.None;
//                     var pick = rnd.NextFloat(0f, 1f);
//                     var subTexIndex = 0;
//                     var prob = 0f;
//                     for (var i = 0;
//                          i < TileTypeToSubTexProbs[(int)tileTypeA].SubTexProbs.Length;
//                          i++) // Last one is no sub tex
//                     {
//                         prob += TileTypeToSubTexProbs[(int)tileTypeA].SubTexProbs[i];
//                         if (pick < prob)
//                         {
//                             subTexIndex = i + 1; // 0 is main tex, so add 1
//                             break;
//                         }
//                     }
//                     aIdx = TileMaterialConfig.indexGrowStepInTexArray * (int)tileTypeA;
//                     bIdx = aIdx + subTexIndex;
//                     noiseWeight = rnd.NextFloat(TileMaterialConfig.subTexNoiseWeightRange.lower,
//                         TileMaterialConfig.subTexNoiseWeightRange.upper);
//                     noiseScale = rnd.NextFloat(TileMaterialConfig.subTexNoiseScaleRange.lower,
//                         TileMaterialConfig.subTexNoiseScaleRange.upper);
//
//                     weightA = 1f;
//
//                 }
//                 else
//                 {
//                     // This is a transition area
//                     tileTypeA = MapLocTypeToTileTypes[(int)locTypeA].Type;
//                     tileTypeB = MapLocTypeToTileTypes[(int)locTypeB].Type;
//                     // Only the main texture is A texture, B is used to add noise
//                     aIdx = weightA > 0.5f ? (int)tileTypeA : (int)tileTypeB ;
//                     bIdx = weightA > 0.5f ? (int)tileTypeB : (int)tileTypeA; 
//                     aIdx *= TileMaterialConfig.indexGrowStepInTexArray;
//                     bIdx *= TileMaterialConfig.indexGrowStepInTexArray;
//                     var w = weightA > 0.5 ?1- weightA :weightA; 
//                     noiseWeight = TileMaterialConfig.transitionNoiseWeightScale *w;
//                     noiseScale = rnd.NextFloat(TileMaterialConfig.transitionNoiseScaleRange.lower, TileMaterialConfig.transitionNoiseScaleRange.upper);
//                 }
//                 var tilingRange = TileTypeToSubTexProbs[(int)tileTypeA].TileTilingRange;
//                 var tiling = rnd.NextFloat(tilingRange.lower, tilingRange.upper);
//
//                 ECB.SetComponent(index, model, new TilingVector4Override
//                 {
//                     Value = tiling
//                 });
//                 ECB.SetComponent(index, model, new OffsetVector4Override
//                 {
//                     Value = new float4(offset.x, offset.y, 0f, 0f)
//                 });
//                 ECB.SetComponent(index, model, new RotationFloatOverride
//                 {
//                     Value = rotation
//                 });
//                 ECB.SetComponent(index, model, new AbTexIndexVector4Override
//                 {
//                     Value = new float4(aIdx, bIdx, 0f, 0f)
//                 });
//                 ECB.SetComponent(index, model, new NoiseScaleFloatOverride
//                 {
//                     Value = noiseScale
//                 });
//                 ECB.SetComponent(index, model, new NoiseWeightFloatOverride
//                 {
//                     Value = noiseWeight
//                 });
//                 data.TypeA = tileTypeA;
//                 data.TypeB = tileTypeB;
//                 data.WeightA = weightA;
//             }
//         }
//     }
// }
//
//

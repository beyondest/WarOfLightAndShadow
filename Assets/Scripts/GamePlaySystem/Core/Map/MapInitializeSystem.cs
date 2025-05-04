using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.General;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

namespace SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GameBasicControlSystem))]
    public partial struct MapInitializeSystem : ISystem
    {

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TileMaterialConfig>();
            state.RequireForUpdate<MapInitInfo>();
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<MapInfo>();
            state.RequireForUpdate<GameStatusData>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var gameStatusData = SystemAPI.GetSingleton<GameStatusData>().Value;
            if (gameStatusData == GameStatus.Init)
            {
                var buffer = SystemAPI.GetSingletonBuffer<MapLocTypeToTileType>();
                buffer.Clear();
                buffer.Add(new MapLocTypeToTileType { Type = TileType.CentralHollow });
                ref var generalRandom = ref SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW;
                MapUtils.AddRandomizedTilesToBuffer(buffer, ref generalRandom.Rnd);
                buffer.Add(new MapLocTypeToTileType { Type = TileType.Rift });
                var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
                new MapInitJob
                {
                    ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                    MapInfoData = SystemAPI.GetSingleton<MapInfo>(),
                    MapLocTypeToTileTypes = buffer,
                    SeedBias = SystemAPI.GetSingletonRW<GeneralRandom>().ValueRW.Rnd.NextInt(),
                    TileMaterialConfig = SystemAPI.GetSingleton<TileMaterialConfig>(),
                    ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                    TileTypeToSubTexProbs = SystemAPI.GetSingletonBuffer<TileTypeToSubTexProbs>()
                }.ScheduleParallel();
            }
        }


        [BurstCompile]
        public partial struct MapInitJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ECB;
            [ReadOnly] public TileMaterialConfig TileMaterialConfig;
            [ReadOnly] public float ElapsedTime;
            [ReadOnly] public int SeedBias;
            [ReadOnly] public MapInfo MapInfoData;
            [ReadOnly] public DynamicBuffer<MapLocTypeToTileType> MapLocTypeToTileTypes;
            [ReadOnly] public DynamicBuffer<TileTypeToSubTexProbs> TileTypeToSubTexProbs;

            private void Execute([EntityIndexInQuery] int index, ref TileData data, in LocalTransform transform,
                in DynamicBuffer<LinkedEntityGroup> children)
            {
                var model = children[1].Value;

                var seed = GeneralUtils.GetSeedByIndexTimeBias(index, SeedBias, ElapsedTime);
                var rnd = new Random(seed);
                // Calculate material override properties
                var tiling = rnd.NextFloat(TileMaterialConfig.tilingRange.lower, TileMaterialConfig.tilingRange.upper);
                var offset = rnd.NextFloat2(TileMaterialConfig.offsetRange.lower, TileMaterialConfig.offsetRange.upper);
                var rotation = rnd.NextFloat(TileMaterialConfig.rotationRange.lower,
                    TileMaterialConfig.rotationRange.upper);
                float noiseWeight;
                float noiseScale;
                float aIdx;
                float bIdx;
                // Calculate the type data
                TileType tileTypeA;
                TileType tileTypeB;


                if (!MapUtils.TryGetMapTileTransition(transform.Position, MapInfoData.WorldCenter,
                        MapInfoData.OuterSquareSize,
                        MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
                        out var locTypeA, out var locTypeB, out var weightA, out _))
                {
                    // If this is not in transition area, randomly choose a sub texture to use as noise
                    var locType = MapUtils.GetMapLocTypeByPos(transform.Position, MapInfoData.WorldCenter,
                        MapInfoData.OuterSquareSize, MapInfoData.InnerSquareSize, MapInfoData.CenterRadius);
                    var mapCenterType = MapLocTypeToTileTypes[(int)locType].Type;
                    tileTypeA = mapCenterType;
                    tileTypeB = TileType.None;
                    var pick = rnd.NextFloat(0f, 1f);
                    var subTexIndex = 0;
                    var prob = 0f;
                    for (var i = 0;
                         i < TileTypeToSubTexProbs[(int)tileTypeA].subTexProbs.Length;
                         i++) // Last one is no sub tex
                    {
                        prob += TileTypeToSubTexProbs[(int)tileTypeA].subTexProbs[i];
                        if (pick < prob)
                        {
                            subTexIndex = i + 1; // 0 is main tex, so add 1
                            break;
                        }
                    }
                    aIdx = TileMaterialConfig.indexGrowStepInTexArray * (int)tileTypeA;
                    bIdx = aIdx + subTexIndex;
                    noiseWeight = rnd.NextFloat(TileMaterialConfig.subTexNoiseWeightRange.lower,
                        TileMaterialConfig.subTexNoiseWeightRange.upper);
                    noiseScale = rnd.NextFloat(TileMaterialConfig.subTexNoiseScaleRange.lower,
                        TileMaterialConfig.subTexNoiseScaleRange.upper);

                    weightA = 1f;

                }
                else
                {
                    // This is transition area
                    tileTypeA = MapLocTypeToTileTypes[(int)locTypeA].Type;
                    tileTypeB = MapLocTypeToTileTypes[(int)locTypeB].Type;
                    // Only the main texture is A texture, B is used to add noise
                    aIdx = weightA > 0.5f ? (int)data.TypeA : (int)data.TypeB ;
                    bIdx = weightA > 0.5f ? (int)data.TypeB : (int)data.TypeA ;
                    noiseWeight = TileMaterialConfig.transitionNoiseWeightScale *(1 - weightA);
                    noiseScale = rnd.NextFloat(TileMaterialConfig.transitionNoiseScaleRange.lower, TileMaterialConfig.transitionNoiseScaleRange.upper);
                }


                ECB.SetComponent(index, model, new TilingVector4Override
                {
                    Value = tiling
                });
                ECB.SetComponent(index, model, new OffsetVector4Override
                {
                    Value = new float4(offset.x, offset.y, 0f, 0f)
                });
                ECB.SetComponent(index, model, new RotationFloatOverride
                {
                    Value = rotation
                });
                ECB.SetComponent(index, model, new AbTexIndexVector4Override
                {
                    Value = new float4(aIdx, bIdx, 0f, 0f)
                });
                ECB.SetComponent(index, model, new NoiseScaleFloatOverride
                {
                    Value = noiseScale
                });
                ECB.SetComponent(index, model, new NoiseWeightFloatOverride
                {
                    Value = noiseWeight
                });
                data.TypeA = tileTypeA;
                data.TypeB = tileTypeB;
                data.WeightA = weightA;
            }
        }
    }
}


/*if (transitionType is TransitionType.Horizontal or TransitionType.Vertical)
{
    ECB.SetComponent(index, model, new ABIdxVector4Override
    {
        Value = new float4(typeAIndex, typeBIndex, 0, 0),
    });
    ECB.SetComponent(index, model, new TilePVector4Override
    {
        Value = new float4(transform.Position.x, transform.Position.y, transform.Position.z, 0f),
    });
    ECB.SetComponent(index, model, new TileSFloatOverride
    {
        Value = TileSize
    });
}
else
{
    ECB.SetComponent(index, model, new CenterPVector4Override
    {
        Value = new float4(MapInfoData.WorldCenter.x, MapInfoData.WorldCenter.y,
            MapInfoData.WorldCenter.z, 0f),
    });
    ECB.SetComponent(index, model, new ABIdxOfRVector4Override
    {
        Value = new float4(typeAIndex, typeBIndex, 0, 0),
    });
    ECB.SetComponent(index, model, new TransLFloatOverride
    {
        Value = MapInfoData.TransitionLength
    });
}

switch (transitionType)
{
    case TransitionType.None:
        // This should never happen
        break;
    case TransitionType.Horizontal:
        var tileMinPointH = new float3(transform.Position.x - TileSize, 0f, transform.Position.z);
        var tileMaxPointH = new float3(transform.Position.x + TileSize, TileSize, 0f);
        if (!MapUtils.TryGetMapTileTransition(tileMinPointH, MapInfoData.WorldCenter,
                MapInfoData.OuterSquareSize,
                MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
                out var _, out var _, out var hMaxWeightA, out var _))
        {
            // XMin is not in transition area, then max weight set to 1 represents pure TileType A
            hMaxWeightA = 1f;
        }

        if (!MapUtils.TryGetMapTileTransition(tileMaxPointH, MapInfoData.WorldCenter,
                MapInfoData.OuterSquareSize,
                MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
                out var _, out var _, out var hMinWeightA, out var _))
        {
            // XMax is not in transition area, then max weight set to 0 represents pure TileType B
            hMinWeightA = 0f;
        }

        ECB.SetComponent(index, model, new WeiAMinMaxVector4Override
        {
            Value = new float4(hMinWeightA, hMaxWeightA, 0, 0),
        });

        break;
    case TransitionType.Vertical:
        var tileMinPointV = new float3(transform.Position.x, 0f, transform.Position.z - TileSize);
        var tileMaxPointV = new float3(transform.Position.x, 0f, transform.Position.z + TileSize);
        if (!MapUtils.TryGetMapTileTransition(tileMinPointV, MapInfoData.WorldCenter,
                MapInfoData.OuterSquareSize,
                MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
                out var _, out var _, out var vMaxWeightA, out var _))
        {
            vMaxWeightA = 1f;
        }

        if (!MapUtils.TryGetMapTileTransition(tileMaxPointV, MapInfoData.WorldCenter,
                MapInfoData.OuterSquareSize,
                MapInfoData.InnerSquareSize, MapInfoData.CenterRadius, MapInfoData.TransitionLength,
                out var _, out var _, out var vMinWeightA, out var _))
        {
            vMinWeightA = 0f;
        }

        ECB.SetComponent(index, model, new WeiAMinMaxVector4Override
        {
            Value = new float4(vMinWeightA, vMaxWeightA, 0, 0),
        });
        break;
    case TransitionType.RoundRing:

        ECB.SetComponent(index, model, new RadiusSubHalfTransLFloatOverride
        {
            Value = MapInfoData.CenterRadius - MapInfoData.TransitionLength * 0.5f
        });
        break;
    case TransitionType.SquareRing:
        ECB.SetComponent(index, model, new RadiusSubHalfTransLFloatOverride
        {
            Value = math.SQRT2 *
                    (MapInfoData.InnerSquareSize * 0.5f - MapInfoData.TransitionLength * 0.5f)
        });

        break;
    default:
        throw new ArgumentOutOfRangeException();
}*/
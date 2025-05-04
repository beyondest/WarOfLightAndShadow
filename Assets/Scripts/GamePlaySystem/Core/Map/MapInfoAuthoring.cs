using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using SparFlame.Utils;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map
{
    public class MapInfoAuthoring : MonoBehaviour
    {
        public MapInitInfo mapInitInfo;
        public TileMaterialConfig tileMaterialConfig;
        public List<TileTypeToSubTexProbs> tileTypeToSubTexProbs;
        
        private class MapInfoAuthoringBaker : Baker<MapInfoAuthoring>
        {
            public override void Bake(MapInfoAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                var sqrtValue = Mathf.Sqrt(authoring.mapInitInfo.tileCount);
                if (Mathf.Abs((int)sqrtValue - sqrtValue) > 0.1f)
                    throw new ArgumentException(
                        "Map info wrong, world must be square, tile count must be sqrt to integer");


                var edgeCount = (int)sqrtValue;
                var edgeLength = edgeCount * authoring.mapInitInfo.tileSize;
                var innerSquareSize = Mathf.Sqrt(1 - authoring.mapInitInfo.riftAreaRatioOfOuterSquare) * edgeLength;
                var centerRadius = Mathf.Sqrt(authoring.mapInitInfo.centerAreaRatioOfInnerSquare / Mathf.PI) *
                                   innerSquareSize;
                var worldCenterX = 0.5f * edgeLength - 0.5f * authoring.mapInitInfo.tileSize;
                AddComponent(entity, authoring.mapInitInfo);
                AddComponent(entity, new MapInfo
                {
                    CenterRadius = centerRadius,
                    InnerSquareSize = innerSquareSize,
                    OuterSquareSize = edgeLength,
                    WorldCenter = new float3(worldCenterX, 0, worldCenterX),
                    TransitionLength = authoring.mapInitInfo.transitionRatioOfOuterEdgeLength * edgeLength
                });
                var buffer = AddBuffer<TileTypeToSubTexProbs>(entity);
                if(authoring.tileTypeToSubTexProbs == null || authoring.tileTypeToSubTexProbs.Count == 0)return;
                var step = authoring.tileTypeToSubTexProbs[0].subTexProbs.Length;
                for (var i = 0; i < authoring.tileTypeToSubTexProbs.Count; i++)
                {
                    var probs = authoring.tileTypeToSubTexProbs[i];
                    var step2 = probs.subTexProbs.Length;
                    if (step2 != step)
                        throw new ArgumentException("Map info init wrong, sub texture counts not same");
                    if (i != (int)probs.tileType)
                        throw new ArgumentException(
                            "Map info init wrong, tile type to sub probs must follow the sequence of enum type");
                    buffer.Add(probs);
                }
                authoring.tileMaterialConfig.indexGrowStepInTexArray = step;
                AddComponent(entity, authoring.tileMaterialConfig);
          
                
     
            }
        }
    }



    public enum TileType
    {
        None = -1,
        CentralHollow = 0,
        Eldergrove = 1,
        ObsidianExpanse = 2,
        AetherPrairie = 3,
        AncientRuins = 4,
        Rift = 5,
    }

    public enum MapLocType
    {
        None = -1,
        CentralCircle = 0,
        InnerSquareUpRight = 1,
        InnerSquareUpLeft = 2,
        InnerSquareDownLeft = 3,
        InnerSquareDownRight = 4,
        SquareRing = 5
    }

    public enum TransitionType
    {
        None = -1,
        Horizontal = 0,
        Vertical = 1,
        RoundRing = 2,
        SquareRing = 3
    }

    [Serializable]
    public struct MapInitInfo : IComponentData
    {
        public float tileSize;
        public int tileCount;
        public float riftAreaRatioOfOuterSquare;
        public float centerAreaRatioOfInnerSquare;
        public float transitionRatioOfOuterEdgeLength;
    }

    [Serializable]
    public struct TileMaterialConfig : IComponentData
    {
        public CustomDs.Range tilingRange;
        public CustomDs.Range offsetRange;
        public CustomDs.Range rotationRange;
        public CustomDs.Range transitionNoiseScaleRange;
        [Tooltip("Use (1 - weightA) * scale = noise weight for transition tile")]
        public float transitionNoiseWeightScale;
        [Tooltip("Random choose noise weight for sub tex mix")]
        public CustomDs.Range subTexNoiseWeightRange;
        public CustomDs.Range subTexNoiseScaleRange;
        public int indexGrowStepInTexArray;
    }

    [Serializable]
    public struct TileTypeToSubTexProbs : IBufferElementData
    {
        public TileType tileType;
        [Tooltip("If tile type is eldergrove, the texture may have 3 tex, Jungle grass," +
                 "Jungle rock, Jungle dirt. First is main tex, sub tex is rock and dirt. If prob rock is 0.2, prob dirt is 0.3," +
                 "then there may be 0.5 prob to be pure grass, 0.2 prob to be grass with rock, 0.3 prob to be grass with dirt. " +
                 "The noise weight is random fro")]
        public FixedList32Bytes<float> subTexProbs;
    }

    public struct MapInfo : IComponentData
    {
        public float3 WorldCenter;
        public float OuterSquareSize;
        public float InnerSquareSize;
        public float CenterRadius;
        public float TransitionLength;
    }

    public struct TileData : IComponentData
    {
        public TileType TypeA;
        public TileType TypeB;
        public float WeightA;
    }
    
    public struct MapLocTypeToTileType : IBufferElementData
    {
        public TileType Type;
    }

    


    
}
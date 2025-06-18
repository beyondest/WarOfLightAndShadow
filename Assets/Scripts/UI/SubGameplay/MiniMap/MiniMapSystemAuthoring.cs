using System;
using Sirenix.OdinInspector;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.Map
{
    public class MiniMapSystemAuthoring : MonoBehaviour
    {
        public Color playerColor;
        public Color enemyColor;
        [SerializeField] private LayerMask miniMapLayerMask;
        public MapInfo mapInfo;
        
        
        private class MiniMapSystemBaker : Baker<MiniMapSystemAuthoring>
        {
            public override void Bake(MiniMapSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MiniMapConfig
                {
                    PlayerColor = new float4(authoring.playerColor.r, authoring.playerColor.g, authoring.playerColor.b, authoring.playerColor.a),
                    EnemyColor = new float4(authoring.enemyColor.r, authoring.enemyColor.g, authoring.enemyColor.b, authoring.enemyColor.a),
                    Layer = (int)math.log2(authoring.miniMapLayerMask.value)
                });
                AddComponent(entity, authoring.mapInfo);
            }
        }
    }

    public struct MiniMapConfig : IComponentData
    {
        public float4 PlayerColor;
        public float4 EnemyColor;
        public int Layer;
    }

    public struct MiniMapInitCompleteTag : IComponentData
    {
        
    }

    [Serializable]
    public struct MapInfo : IComponentData
    {
        public float outerSquareSize;
        public float tileSize;
    }
    
}
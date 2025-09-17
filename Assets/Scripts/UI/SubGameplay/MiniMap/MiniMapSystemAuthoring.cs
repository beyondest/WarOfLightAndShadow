using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.Systems.Map
{
    public class MiniMapSystemAuthoring : MonoBehaviour
    {
        public Color playerColor;
        public Color allyColor;
        public Color hostileColor;
        public Color neutralColor;
        [SerializeField] private LayerMask miniMapLayerMask;
        [SerializeField] private float miniMapCameraHeight;
        
        private class MiniMapSystemBaker : Baker<MiniMapSystemAuthoring>
        {
            public override void Bake(MiniMapSystemAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new MiniMapConfig
                {
                    PlayerColor = new float4(authoring.playerColor.r, authoring.playerColor.g, authoring.playerColor.b, authoring.playerColor.a),
                    AllyColor = new float4(authoring.allyColor.r, authoring.allyColor.g, authoring.allyColor.b, authoring.allyColor.a),
                    NeutralColor = new float4(authoring.neutralColor.r, authoring.neutralColor.g, authoring.neutralColor.b, authoring.neutralColor.a),
                    HostileColor = new float4(authoring.hostileColor.r, authoring.hostileColor.g, authoring.hostileColor.b, authoring.hostileColor.a),
                    Layer = (int)math.log2(authoring.miniMapLayerMask.value),
                    MiniMapCameraHeight = authoring.miniMapCameraHeight
                });
            }
        }
    }

    public struct MiniMapConfig : IComponentData
    {
        public float4 PlayerColor;
        public float4 AllyColor;
        public float4 HostileColor;
        public float4 NeutralColor;
        public int Layer;
        public float MiniMapCameraHeight;
    }

    public struct MiniMapInitCompleteTag : IComponentData
    {
        
    }


    
}
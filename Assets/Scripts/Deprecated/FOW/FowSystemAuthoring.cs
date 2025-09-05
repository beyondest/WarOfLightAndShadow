// using System;
// using SparFlame.GamePlaySystem.CameraControl;
// using Unity.Entities;
// using Unity.Mathematics;
// using UnityEngine;
// using UnityEngine.Serialization;
//
//
// namespace SparFlame.GamePlaySystem.Fow
// {
//     public class FowSystemAuthoring : MonoBehaviour
//     {
//         [SerializeField] private bool refresh;
//          private GameObject _fogOfWarGo;
//         
//        [SerializeField] [Tooltip("Size of the fog of war RenderTexture that will be projected with the Plane")]
//         private int fowTextureSize = 2048;
//          [FormerlySerializedAs("fowColor")] [SerializeField] [Tooltip("Color of the fog of war")]
//         private Color lightFowColor = new(0.1f, 0.1f, 0.1f, 0.7f);
//         [SerializeField] [Tooltip("Color of the fog of war")]
//         private Color darkFowColor = new(0.1f, 0.1f, 0.1f, 0.7f);
//         
//         
//         [Range(0.01f, 1.0f)] [SerializeField] [Tooltip("How frequently will the fog of war be updated?")]
//         private float updateInterval = 0.02f;
//         
//         [Range(0.0f, 3.0f)]
//         [SerializeField]
//         [Tooltip("How much will the blocked sight be 'pushed away' to prevent flickers on vertical obstacles?")]
//         private float blockOffset = 1.0f;
//
//         [Range(1.0f, 100.0f)]
//         [SerializeField]
//         [Tooltip("Deviation of the Gaussian filter(larger value strengthens filtering effect to some extent)")]
//         private float sigma = 30.0f;
//
//         [Range(0, 10)]
//         [SerializeField]
//         [Tooltip(
//             "How many times will the Gaussian filter be applied? More iterations lead to a smoother fog of war, but with worse performance.")]
//         private int blurIterationCount = 1;
//
//         
//         [SerializeField]
//         private int maxEnemyCount = 500;
//         
//         [SerializeField]
//         private int maxAllyCount = 500;
//
//
//         private Vector3 Position => _fogOfWarGo ? _fogOfWarGo.transform.position : Vector3.zero;
//         private Vector3 Forward => _fogOfWarGo ? _fogOfWarGo.transform.forward : Vector3.forward;
//         private Vector3 Right => _fogOfWarGo ? _fogOfWarGo.transform.right : Vector3.right;
//         private Vector3 LossyScale => _fogOfWarGo ? _fogOfWarGo.transform.lossyScale : Vector3.one;
//         private Vector3 LocalScale => _fogOfWarGo ? _fogOfWarGo.transform.localScale : Vector3.one;
//         
//         
//         private class MyFowManagerAuthoringBaker : Baker<FowSystemAuthoring>
//         {
//             public override void Bake(FowSystemAuthoring authoring)
//             {
//                 var go = FindAnyObjectByType<FogOfWarTagAuthoring>();
//                 if(go == null)
//                 {
//                     throw new ArgumentException("No fogOfWarGoTag found.");
//                 }
//
//                 authoring._fogOfWarGo = go.gameObject;
//                 var entity = GetEntity(TransformUsageFlags.None);
//                 AddComponent(entity, new FowConfig
//                 {
//                     FowTextureSize = authoring.fowTextureSize,
//                     LightFowColor = new float4(authoring.lightFowColor.r,authoring.lightFowColor.g,
//                         authoring.lightFowColor.b,authoring.lightFowColor.a),
//                     DarkFowColor = new float4(authoring.darkFowColor.r, authoring.darkFowColor.g,
//                         authoring.darkFowColor.b, authoring.darkFowColor.a),
//                     BlockOffset = authoring.blockOffset,
//                     BlurIterationCount = authoring.blurIterationCount,
//                     Sigma = authoring.sigma,
//                     UpdateInterval = authoring.updateInterval,
//                     MaxEnemyCount = authoring.maxEnemyCount,
//                     MaxAllyCount = authoring.maxAllyCount,
//                     
//                     Forward = authoring.Forward,
//                     Right = authoring.Right,
//                     LocalScale = authoring.LocalScale,
//                     LossyScale = authoring.LossyScale,
//                     Position = authoring.Position,
//                 });
//                 
//                 
//                 var mousePosEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
//                 AddComponent<MousePositionFowTag>(mousePosEntity);
//                 AddComponent(mousePosEntity, new FowAgentData
//                 {
//                     SightRange = 0,
//                     IsInsight = true
//                 });
//                 AddComponent(mousePosEntity, new ScreenPos
//                 {
//                     ScreenPosition = float2.zero
//                 });
//                 AddComponent<InCameraView>(mousePosEntity);
//                 AddComponent<InCameraExtendView>(mousePosEntity);
//                 SetComponentEnabled<InCameraView>(mousePosEntity, false);
//                 SetComponentEnabled<InCameraExtendView>(mousePosEntity, false);
//                 AddComponent<DisappearInFowTag>(mousePosEntity);
//                 AddComponent<InDarknessTag>(mousePosEntity);
//                 SetComponentEnabled<InDarknessTag>(mousePosEntity, false);
//             }
//         }
//     }
//
//     public struct FowConfig : IComponentData
//     {
//         public int FowTextureSize;
//         public float4 LightFowColor;
//         public float4 DarkFowColor;
//         public float UpdateInterval;
//         public float BlockOffset;
//         public float Sigma;
//         public int BlurIterationCount;
//         public int MaxEnemyCount;
//         public int MaxAllyCount;
//         
//         
//         public float3 Position;
//         public float3 LossyScale;
//         public float3 Forward;
//         public float3 Right;
//         public float3 LocalScale;
//
//     }
//
//     public struct MousePositionFowTag : IComponentData
//     {
//     }
//
//     public struct InDarknessTag : IComponentData,IEnableableComponent
//     {
//         
//     }
//     
// }
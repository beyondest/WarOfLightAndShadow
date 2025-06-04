// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Rendering;
// using UnityEngine;
//
// namespace SparFlame.GamePlaySystem.Map
// {
//     public class TileMaterialOverrideAuthoring : MonoBehaviour
//     {
//         private class TileMaterialOverrideAuthoringBaker : Baker<TileMaterialOverrideAuthoring>
//         {
//             public override void Bake(TileMaterialOverrideAuthoring authoring)
//             {
//                 var entity = GetEntity(TransformUsageFlags.Renderable);
//
//
//                 /*// HV transition Material Override
//                 AddComponent<ABIdxVector4Override>(entity);
//                 AddComponent<TilePVector4Override>(entity);
//                 AddComponent<TileSFloatOverride>(entity);
//                 AddComponent<WeiAMinMaxVector4Override>(entity);
//
//
//                 // R transition Material Override
//                 AddComponent<ABIdxOfRVector4Override>(entity);
//                 AddComponent<CenterPVector4Override>(entity);
//                 AddComponent<RadiusSubHalfTransLFloatOverride>(entity);
//                 AddComponent<TransLFloatOverride>(entity);*/
//
//                 // Pure transition Override
//                 AddComponent<OffsetVector4Override>(entity);
//                 AddComponent<RotationFloatOverride>(entity);
//                 AddComponent<TilingVector4Override>(entity);
//                 AddComponent<AbTexIndexVector4Override>(entity);
//
//
//                 AddComponent<NoiseScaleFloatOverride>(entity);
//                 AddComponent<NoiseWeightFloatOverride>(entity);
//
//                 var defaultPos = new float4(0f, -1f, 0f, 0f);
//                 AddComponent(entity, new CrystalPosVector4Override
//                 {
//                     Value = defaultPos
//                 });
//                 AddComponent(entity, new CrystalPos2Vector4Override
//                 {
//                     Value = defaultPos
//                 });
//                 AddComponent(entity, new CrystalPos3Vector4Override
//                 {
//                     Value = defaultPos
//                 });
//                 AddComponent(entity, new CrystalPos4Vector4Override
//                 {
//                     Value = defaultPos
//                 });
//                 AddComponent(entity, new IsLightFloatOverride
//                 {
//                         Value = 0f
//                 });
//                 AddComponent(entity, new CrystalRadiusFloatOverride
//                 {
//                     Value = 0f
//                 });
//             }
//         }
//     }
// }
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace SparFlame.GamePlaySystem.Map.GamePlaySystem.Core.Map
{
    public class TileDataAuthoring : MonoBehaviour
    {
        private class TileTypeAuthoringBaker : Baker<TileDataAuthoring>
        {
            public override void Bake(TileDataAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new TileData());
                
                /*// HV transition Material Override
                AddComponent<ABIdxVector4Override>(entity);
                AddComponent<TilePVector4Override>(entity);
                AddComponent<TileSFloatOverride>(entity);
                AddComponent<WeiAMinMaxVector4Override>(entity);
                
                
                // R transition Material Override
                AddComponent<ABIdxOfRVector4Override>(entity);
                AddComponent<CenterPVector4Override>(entity);
                AddComponent<RadiusSubHalfTransLFloatOverride>(entity);
                AddComponent<TransLFloatOverride>(entity);*/
                
                // Pure transition Override
                AddComponent<OffsetVector4Override>(entity);
                AddComponent<RotationFloatOverride>(entity);
                AddComponent<TilingVector4Override>(entity);
                AddComponent<AbTexIndexVector4Override>(entity);
                
                AddComponent(entity,
                    new IsLightFloatOverride
                    {
                        Value = 0f
                    });
                AddComponent<NoiseScaleFloatOverride>(entity);
                AddComponent<NoiseWeightFloatOverride>(entity);
                
                var defaultPos = new float4(0f, -1f, 0f, 0f);
                AddComponent<CrystalPosVector4Override>(entity,new CrystalPosVector4Override
                {
                    Value = defaultPos
                });
                AddComponent<CrystalPos2Vector4Override>(entity, new CrystalPos2Vector4Override
                {
                    Value = defaultPos
                });
                AddComponent<CrystalPos3Vector4Override>(entity, new CrystalPos3Vector4Override
                {
                    Value = defaultPos
                });
                AddComponent<CrystalPos4Vector4Override>(entity, new CrystalPos4Vector4Override
                {
                    Value = defaultPos
                });
                AddComponent(entity, new CrystalRecordData
                {
                    RecordCount = 0
                });
            }
        }
    }

    public struct CrystalRecordData : IComponentData
    {
        public int RecordCount;
    }

}
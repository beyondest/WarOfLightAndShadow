using System;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace SparFlame.Systems.General.BasicControl
{

    [Serializable]
    public struct SeGlobalId : IComponentData
    {
        public int value;
    }

    [Serializable]
    public struct SeTransform : IComponentData
    {
        public float3 position;
        public quaternion rotation;
        public float scale;
    }

    [Serializable]
    public struct SeInverseMass : IComponentData
    {
        public float value;
    }
    
    [Serializable]
    public struct SeInGarrison : IComponentData
    {
        public long buildingTmpId;
        public bool inBuilding;
        public float priorMass;
    }


    [Serializable]
    public struct SeGarrisonEntity : IBufferElementData
    {
        public long unitTmpId;
        public int id;
    }

    [Serializable]
    public struct SeTmpId : IComponentData
    {
        public long value;
    }
    
    
    [BurstCompile]
    [WithNone(typeof(InArmyGroup))]
    public partial struct SaveSubGameplayJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        
        [ReadOnly] public BufferLookup<GarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;
        [ReadOnly] public ComponentLookup<InGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;

        private void Execute([ChunkIndexInQuery]int index, in SubGameplayGeneralAttr generalAttr, in LocalTransform transform,
            in StatData statData, in ExpData expData, Entity selfEntity)
        {
            var saveEntity = ECB.CreateEntity(index);
            ECB.AddComponent(index, saveEntity,new SeTransform
            {
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale,
            });
            ECB.AddComponent(index, saveEntity, new SeGlobalId{value = generalAttr.ID});
            ECB.AddComponent(index, saveEntity, statData);
            if (generalAttr.BaseTag == BaseTag.Units)
            {
                ECB.AddComponent(index, saveEntity, expData);
                if (InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison)
                    && GarrisonEntitiesLookup.HasBuffer(inGarrison.BuildingEntity))// Safety check
                {
                    var physicsMass = PhysicsMassLookup[selfEntity];
                    ECB.AddComponent(index, saveEntity, new SeInverseMass{value = physicsMass.InverseMass});
                    ECB.AddComponent(index, saveEntity, new SeInGarrison
                    {
                        buildingTmpId = SaveUtilities.GetTmpIdForSaving(inGarrison.BuildingEntity),
                        inBuilding = inGarrison.InBuilding,
                        priorMass = inGarrison.PriorMass,
                    });
                    ECB.AddComponent(index, saveEntity, new SeTmpId{value = SaveUtilities.GetTmpIdForSaving(selfEntity)});
                    
                }
            }
            else if (generalAttr.BaseTag == BaseTag.Buildings)
            {
                if (GarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities)
                    && garrisonEntities.Length > 0)
                {
                    var garrisonTypeDataBuffer = GarrisonTypeDataLookup[selfEntity];
                    ECB.AddBuffer<GarrisonTypeData>(index, saveEntity);
                    foreach (var typeData in garrisonTypeDataBuffer)
                    {
                        ECB.AppendToBuffer(index, saveEntity, typeData);
                    }

                    ECB.AddBuffer<SeGarrisonEntity>(index, saveEntity);
                    foreach (var garrisonEntity in garrisonEntities)
                    {
                        ECB.AppendToBuffer(index, saveEntity, new SeGarrisonEntity
                        {
                            unitTmpId = SaveUtilities.GetTmpIdForSaving(garrisonEntity.Value),
                            id = garrisonEntity.Id
                        });
                    }
                    ECB.AddComponent(index, saveEntity, new SeTmpId{value = SaveUtilities.GetTmpIdForSaving(selfEntity)});
                }
            }
        }
    }
    
    [BurstCompile]
    public partial struct LoadSubGameplayJob : IJobEntity
    {
        
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<ExpData> ExpLookup;
        [ReadOnly] public ComponentLookup<SeInGarrison> InGarrisonLookup;
        [ReadOnly] public ComponentLookup<SeInverseMass> InverseMassLookup;
        [ReadOnly] public ComponentLookup<PhysicsMass> PhysicsMassLookup;
        [ReadOnly] public BufferLookup<SeGarrisonEntity> GarrisonEntitiesLookup;
        [ReadOnly] public BufferLookup<GarrisonTypeData> GarrisonTypeDataLookup;
        [ReadOnly] public ComponentLookup<SeTmpId> TmpIdLookup;
        [ReadOnly] public NativeHashMap<int, Entity> GlobalIdxToPrefabs;
        
        private void Execute([ChunkIndexInQuery]int index,
            in SeGlobalId globalId, in SeTransform transform, in StatData statData, Entity selfEntity)
        {
            ECB.DestroyEntity(index, selfEntity);
            
            // Create instance and set general data
            var instance = ECB.Instantiate(index,GlobalIdxToPrefabs[globalId.value] );
            ECB.AddComponent<SubGameplayEntityTag>(index, instance);
            ECB.SetComponent(index, instance, new LocalTransform
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.scale,
            });
            ECB.SetComponent(index, instance, statData);
            
            // Set exp data
            if (ExpLookup.TryGetComponent(selfEntity, out var exp))
            {
                ECB.SetComponent(index, instance, exp);
            }
            
            // Set garrison unit data
            if (InGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, instance, inGarrison);
                var physicsMass = PhysicsMassLookup[GlobalIdxToPrefabs[globalId.value]];
                physicsMass.InverseMass = InverseMassLookup[selfEntity].value;
                ECB.SetComponent(index, instance, physicsMass);
            }

            // Set garrisoned building buffer
            if (GarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                ECB.AddBuffer<SeGarrisonEntity>(index, instance);
                foreach (var garrisonEntity in garrisonEntities)
                {
                    ECB.AppendToBuffer(index, instance, garrisonEntity);
                }

                foreach (var typeData in GarrisonTypeDataLookup[selfEntity])
                {
                    ECB.AppendToBuffer(index, instance, typeData);
                }
            }
            
            // Set tmp id
            if (TmpIdLookup.TryGetComponent(selfEntity, out var tmpId))
            {
                ECB.AddComponent(index, instance, tmpId);
            }
        }
    }

    [BurstCompile]
    [WithAll(typeof(SeTmpId))]
    public partial struct ReplaceTmpIdJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public NativeHashMap<long, Entity> TmpIdxToInstances;
        [ReadOnly] public ComponentLookup<SeInGarrison> SeInGarrisonLookup;
        [ReadOnly] public BufferLookup<SeGarrisonEntity> SeGarrisonEntitiesLookup;
        private void Execute([ChunkIndexInQuery]int index, Entity selfEntity)
        {
            ECB.RemoveComponent<SeTmpId>(index, selfEntity);
            if (SeInGarrisonLookup.TryGetComponent(selfEntity, out var inGarrison))
            {
                ECB.AddComponent(index, selfEntity, new InGarrison
                {
                    BuildingEntity = TmpIdxToInstances[inGarrison.buildingTmpId],
                    InBuilding = inGarrison.inBuilding,
                    PriorMass = inGarrison.priorMass,
                });
                ECB.RemoveComponent<SeInGarrison>(index, selfEntity);
            }

            if (SeGarrisonEntitiesLookup.TryGetBuffer(selfEntity, out var garrisonEntities))
            {
                foreach (var seGarrison in garrisonEntities)
                {
                    ECB.AppendToBuffer(index, selfEntity, new GarrisonEntity
                    {
                        Id = seGarrison.id,
                        Value = TmpIdxToInstances[seGarrison.unitTmpId]
                    });
                }
                ECB.RemoveComponent<SeGarrisonEntity>(index, selfEntity);
            }
        }
    }
}
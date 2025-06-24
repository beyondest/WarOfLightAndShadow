using SparFlame.Systems.General.BasicControl;
using Unity.Burst;
using Unity.Entities;
using Unity.Entities.Serialization;
using UnityEngine;
// ReSharper disable Unity.Entities.SingletonMustBeRequested

namespace SparFlame.Test
{
    public partial struct TestSavingSystem : ISystem
    {
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TestSavingTag>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var path = "E:\\Download\\1.sav";
            if (Input.GetKeyDown(KeyCode.S))
            {
                using (var serializeWorld = new World("Serialization World"))
                {
                    EntityManager seEm = serializeWorld.EntityManager;
                    // ecb.Playback(seEm);
                    // ecb.Dispose();
                    seEm.CreateSingleton(new SaveSystemPlus.SaveTmpTag());
                    seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                    seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                    // Save
                    using (var writer =
                           new StreamBinaryWriter(path))
                    {
                        SerializeUtility.SerializeWorld(seEm, writer);
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                using (var deserializeWorld = new World("Deserialization World"))
                {
                    var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                    using (var reader =
                           new StreamBinaryReader(path))
                    {
                        SerializeUtility.DeserializeWorld(transaction, reader);
                    }
                    deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    state.EntityManager.MoveEntitiesFrom(deserializeWorld.EntityManager);
                    state.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SaveSystemPlus.SaveTmpTag>());
                }
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                var e = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(e, new TestTag{ID = (int)SystemAPI.Time.ElapsedTime});
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                using (var serializeWorld = new World("Serialization World"))
                {
                    var seEm = serializeWorld.EntityManager;
                    var e = seEm.CreateEntity();
                    seEm.AddComponent<TestTag>(e);
                    seEm.CreateSingleton(new SaveSystemPlus.SaveTmpTag());
                    seEm.RemoveComponent<SceneTag>(seEm.UniversalQuery);
                    seEm.RemoveComponent<SceneSection>(seEm.UniversalQuery);
                    // Save
                    using (var writer =
                           new StreamBinaryWriter(path))
                    {
                        SerializeUtility.SerializeWorld(seEm, writer);
                    }
                }
            }
        }

      
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace SparFlame.Systems.General.BasicControl
{
    [BurstCompile]
    public partial struct UnitSavePreProcessJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        [ReadOnly] public ComponentLookup<GlobalSingleId> SingleIdLookup;

        private void Execute([ChunkIndexInQuery] int index, InGarrison inGarrison, Entity selfEntity)
        {
            ECB.AddComponent(index, selfEntity, new SeInGarrison
            {
                buildingSingleId = SingleIdLookup[inGarrison.BuildingEntity].value
            });
        }
    }

    public struct NeedSaveTag : IComponentData
    {
    }

    public enum SaveType
    {
        None = 0,
        Unit = 1 << 0,
        ArmyGroup = 1 << 1,
        City = 1 << 2,
    }

    [Serializable]
    public class SavableArchetype
    {
        public int ArchetypeId; // 区分不同 Archetype
        public List<Type> ComponentTypes = new(); // 需要保存的组件类型
    }

    public class SavableArchetypeConfig : IComponentData
    {
        public Dictionary<SaveType, SavableArchetype> SaveTypeToArchetype = new();
    }

    public interface ISavePreProcessor
    {
        Task RunAsync(EntityCommandBuffer ecb, ComponentLookup<GlobalSingleId> singleIdLookup,
            JobHandle dependency);
    }

    public class UnitSavePreProcessor : ISavePreProcessor
    {
        public async Task RunAsync(EntityCommandBuffer ecb, ComponentLookup<GlobalSingleId> singleIdLookup,
            JobHandle dependency
        )
        {
            var job = new UnitSavePreProcessJob
            {
                ECB = ecb.AsParallelWriter(),
                SingleIdLookup = singleIdLookup
            }.ScheduleParallel(dependency);
            while (!job.IsCompleted)
            {
                await Task.Yield();
            }
        }
    }

    public class SavePreProcessorFactory
    {
        private readonly Dictionary<SaveType, ISavePreProcessor> _preProcessors = new();

        public SavePreProcessorFactory()
        {
            _preProcessors[SaveType.Unit] = new UnitSavePreProcessor();
        }

        public ISavePreProcessor GetPreProcessor(SaveType saveType)
        {
            return _preProcessors[saveType];
        }
    }

    public partial class SaveSystemAsync : SystemBase
    {
        private EntityQuery _saveQuery;
        private ComponentLookup<GlobalSingleId> _singleIdLookup;
        private SavePreProcessorFactory _savePreProcessorFactory;

        protected override void OnCreate()
        {
            _singleIdLookup = GetComponentLookup<GlobalSingleId>(true);
            _savePreProcessorFactory = new SavePreProcessorFactory();
        }

        protected override void OnStartRunning()
        {
        }

        protected override void OnUpdate()
        {
        }


        private async Task PreProcess(SaveType saveType)
        {
            using var ecb = new EntityCommandBuffer(Allocator.Persistent);
            _singleIdLookup.Update(this);
            var preProcessor = _savePreProcessorFactory.GetPreProcessor(saveType);
            await preProcessor.RunAsync(ecb, _singleIdLookup, Dependency);
            ecb.Playback(EntityManager);
        }


        private async Task Save(SaveType saveType)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var savePath = SaveUtilities.GetCityMainDataPath(playerSaveSlot);
            var config = SystemAPI.ManagedAPI.GetSingleton<SavableArchetypeConfig>();

            await PreProcess(saveType);
            using var entities = _saveQuery.ToEntityArray(Allocator.Persistent);
            var generalAttrs = _saveQuery.ToComponentDataArray<SubGameplayGeneralAttr>(Allocator.Persistent);
            await using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            var template = config.SaveTypeToArchetype[saveType];

            for (var i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                int prefabId = generalAttrs[i].PrefabID;
                await fs.WriteAsync(BitConverter.GetBytes(template.ArchetypeId), 0, 4); // Write archetype id
                await fs.WriteAsync(BitConverter.GetBytes(prefabId), 0, 4); // Write prefab id

                foreach (var t in template.ComponentTypes)
                {
                    object boxed = SaveUtilities.GetComponentBoxed(EntityManager, e, t);
                    byte[] bytes = SaveUtilities.StructToBytes(boxed, t);
                    await fs.WriteAsync(BitConverter.GetBytes(bytes.Length), 0, 4); // Write length of bytes
                    await fs.WriteAsync(bytes, 0, bytes.Length);
                }
            }
        }


        private async Task SavePlus(SaveType saveType)
        {
            var playerSaveSlot = SystemAPI.GetSingleton<PlayerSaveSlot>().Value;
            var savePath = SaveUtilities.GetCityMainDataPath(playerSaveSlot);
            var tmpPath = savePath + ".tmp"; // 原子写入临时文件
            var config = SystemAPI.ManagedAPI.GetSingleton<SavableArchetypeConfig>();

            await PreProcess(saveType);

            using var entities = _saveQuery.ToEntityArray(Allocator.Persistent);
            var generalAttrs = _saveQuery.ToComponentDataArray<SubGameplayGeneralAttr>(Allocator.Persistent);
            var template = config.SaveTypeToArchetype[saveType];

            // 先写入 tmp 文件
            await using (var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                for (var i = 0; i < entities.Length; i++)
                {
                    var e = entities[i];
                    int prefabId = generalAttrs[i].PrefabID;

                    await fs.WriteAsync(BitConverter.GetBytes(template.ArchetypeId), 0, 4);
                    await fs.WriteAsync(BitConverter.GetBytes(prefabId), 0, 4);

                    foreach (var t in template.ComponentTypes)
                    {
                        // 通过反射调用 SystemAPI.Query / ToComponentDataArray<T>()
                        var method = typeof(EntityQuery)
                            .GetMethods(BindingFlags.Public | BindingFlags.Static)
                            .First(m => m.Name == "ToComponentDataArray" && m.IsGenericMethod &&
                                        m.GetParameters().Length == 2)
                            .MakeGenericMethod(t);

                        using var nativeArray = (IDisposable)method.Invoke(
                            null,
                            new object[] { _saveQuery, Allocator.Temp }
                        );

                        // 把 NativeArray<T> 转换成 byte[]
                        byte[] bytes = SaveUtilities.NativeArrayToBytes(nativeArray, t, i);
                        await fs.WriteAsync(BitConverter.GetBytes(bytes.Length), 0, 4);
                        await fs.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
            }

            // 原子替换：完成后再覆盖
            if (File.Exists(savePath))
                File.Delete(savePath);
            File.Move(tmpPath, savePath);
        }
    }
}
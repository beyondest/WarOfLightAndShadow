using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Test
{
    // ========== 组件定义 ==========
    public struct SavingTag : IComponentData
    {
    }

    public struct PrefabId : IComponentData
    {
        public int Value;
    }


    // ========== 模板定义 ==========
    [Serializable]
    public class SavableArchetype
    {
        public int ArchetypeId; // 区分不同 Archetype
        public List<Type> ComponentTypes = new(); // 需要保存的组件类型
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance;
        public List<SavableArchetype> ArchetypeTemplates = new(); // 编辑器里配置
        public Dictionary<int, Entity> PrefabRegistry = new(); // prefabId → prefab entity

        private string savePath => Path.Combine(Application.persistentDataPath, "save.bin");

        private void Awake()
        {
            Instance = this;
            ArchetypeTemplates.Clear();
            ArchetypeTemplates.Add(new SavableArchetype
            {
                ArchetypeId = 0,
                ComponentTypes = new List<Type>()
                {
                    typeof(TestSavingComponentA),
                    typeof(TestSavingComponentB)
                }
            });
            Debug.Log($"{savePath}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.S))
                _ = SaveAsync();
            if (Input.GetKeyDown(KeyCode.L))
                _ = LoadAsync();
        }


// Save: 对每个 entity，遍历 template.ComponentTypes，用 GetComponentBoxed 拿到 boxed struct，再用 StructToBytes 写文件
        private async Task SaveAsync()
        {
            using var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            foreach (var template in ArchetypeTemplates)
            {
                // 构造 EntityQuery：SavingTag + PrefabId + template.ComponentTypes
                // var qb = new EntityQueryDesc { All = new ComponentType[] { typeof(SavingTag), typeof(PrefabId) } };
                // 这里简化：请用 QueryBuilder 或动态构建，根据 template.ComponentTypes 构建查询
                // ...
                var em = World.DefaultGameObjectInjectionWorld.EntityManager;
                var query = em.CreateEntityQuery(typeof(NeedSaveTag));

                var entities = query.ToEntityArray(Allocator.Persistent);

                foreach (var e in entities)
                {
                    int prefabId = 0;
                    // 写 archetypeId + prefabId（4 bytes each）
                    await fs.WriteAsync(BitConverter.GetBytes(template.ArchetypeId), 0, 4);
                    await fs.WriteAsync(BitConverter.GetBytes(prefabId), 0, 4);

                    // 对每个组件类型写入其 bytes（注意：写长度或不写长度由你协议定）
                    foreach (var t in template.ComponentTypes)
                    {
                        // 获取 boxed struct
                        object boxed = ComponentReflectionUtil.GetComponentBoxed(em, e, t);
                        // 转 bytes
                        byte[] bytes = ComponentReflectionUtil.StructToBytes(boxed, t);
                        // 可写入 size+data
                        await fs.WriteAsync(BitConverter.GetBytes(bytes.Length), 0, 4);
                        await fs.WriteAsync(bytes, 0, bytes.Length);
                    }
                }
                entities.Dispose();
            }
        }

// Load：读取 archetypeId + prefabId，Instantiate prefab，然后按 template 里的 ComponentTypes 依次读 size+data -> BytesToStruct -> SetComponentBoxed
        private async Task LoadAsync()
        {
            if (!File.Exists(savePath)) return;
            var fileBytes = await File.ReadAllBytesAsync(savePath);
            int offset = 0;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;

            while (offset < fileBytes.Length)
            {
                int archetypeId = BitConverter.ToInt32(fileBytes, offset);
                offset += 4;
                int prefabId = BitConverter.ToInt32(fileBytes, offset);
                offset += 4;

                var template = ArchetypeTemplates.Find(t => t.ArchetypeId == archetypeId);
                if (template == null) throw new Exception("template not found");

                // Entity prefab = PrefabRegistry[prefabId];
                var entity = em.CreateEntity();
                em.AddComponent<TestSavingComponentA>(entity);
                em.AddComponent<TestSavingComponentB>(entity);
                foreach (var t in template.ComponentTypes)
                {
                    int size = BitConverter.ToInt32(fileBytes, offset);
                    offset += 4;
                    var data = new byte[size];
                    Buffer.BlockCopy(fileBytes, offset, data, 0, size);
                    offset += size;

                    object boxed = ComponentReflectionUtil.BytesToStruct(data, t);
                    ComponentReflectionUtil.SetComponentBoxed(em, entity, boxed, t);
                }
            }
        }



    }
}
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using SparFlame.Systems.General.BasicControl;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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
        public int ArchetypeId;

        [SerializeField] private List<string> componentTypeNames = new();

        // 缓存
        [NonSerialized] private List<Type> _types = new();
        [NonSerialized] private List<ComponentType> _componentTypes = new();

        public List<Type> Types => _types;
        public List<ComponentType> ComponentTypes => _componentTypes;

        private static readonly Dictionary<string, Type> TypeNameToType = new()
        {
            { "TestSavingComponentA", typeof(TestSavingComponentA) },
            { "TestSavingComponentB", typeof(TestSavingComponentB) },
        };
        
        // --- 序列化回调 ---
       

        // 手动触发（比如运行时动态修改了 componentTypeNames 时）
        public void RebuildCache()
        {
            _types = componentTypeNames
                .Select(name => TypeNameToType.GetValueOrDefault(name))
                .Where(t => t != null)
                .ToList();

            _componentTypes = _types
                .Select(ComponentType.ReadWrite)
                .ToList();
        }

        public SavableArchetype(List<ComponentType> types, int archetypeId)
        {
            ArchetypeId = archetypeId;
            _componentTypes = types;
            foreach (var type in types)
            {
                _types.Add(type.GetType());
            }
        }
    }


   
}
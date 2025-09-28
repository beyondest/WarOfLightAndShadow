using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GamePlaySystem.Database;
using Sirenix.OdinInspector;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using UnityEngine;
using Range = SparFlame.Core.Structs.Range;

namespace SparFlame.Database
{
    [CreateAssetMenu(fileName = "ResourceDatabase", menuName = "GameData/ResourceDatabase", order = 0)]
    public class ResourceDatabaseSo :GeneralDatabase<ResourceDataItem>
    {
        
        public override List<ResourceDataItem> Items => items;

        [SerializeReference, TableList(ShowIndexLabels = true), HideLabel,ListDrawerSettings(DraggableItems = true)]
        private List<ResourceDataItem> items;
        
        [SerializeField, ValueDropdown(nameof(GetTypeOptions))]
        private string selectedTypeName;

        private IEnumerable<string> GetTypeOptions()
        {
            return GetAllTypes().Select(t => t.FullName);
        }
        private IEnumerable<Type> GetAllTypes()
        {
            return Assembly.GetAssembly(typeof(ResourceDataItem))
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(ResourceDataItem)) && !t.IsAbstract);
        }
        [Button("Add Resource")]
        private void AddSelectedUnit()
        {
            if (string.IsNullOrEmpty(selectedTypeName))
            {
                if (Activator.CreateInstance(typeof(ResourceDataItem)) is ResourceDataItem data)
                {
                    items.Add(data);
                }
                return;
            }
            var type = Type.GetType(selectedTypeName);
            if (type == null)
            {
                Debug.LogError($"Type not found: {selectedTypeName}");
                return;
            }
            if (Activator.CreateInstance(type) is ResourceDataItem instance)
            {
                items.Add(instance);
            }
        }

        [Button("Check Config Valid")]
        private void CheckConfigValid()
        {
            var types = new HashSet<ResourceType>();
            foreach (var data in items)
            {
                types.Add(data.type);
            }
            foreach (ResourceType type in types)
            {
                var sameTypes = Items.Where(dataItem => dataItem.type == type).ToList();
                var totalProb = sameTypes.Sum(item => item.prob);
                if (!Mathf.Approximately(totalProb, 1f))
                {
                    foreach (var item in sameTypes)
                    {
                        Debug.Log($"{item.id} {item.prob}");
                    }
                    throw new ArgumentException($"Resource {type} total probabilities is not 1f, {totalProb}");
                }
            }
        }
        
    }


    [Serializable]
    public class ResourceDataItem :GeneralDataItem
    {
        [VerticalGroup("EnumValues"), HideLabel, Tooltip("resource type")]
        public ResourceType type;

        [FoldoutGroup("Gameplay/Resource"), HorizontalGroup("Gameplay/Resource/0"), Tooltip("resource amount range, " +
             "this is used for system automatically regenerate resource")]
        public Range amountRange;
        
        [FoldoutGroup("Gameplay/Resource"), HorizontalGroup("Gameplay/Resource/1"), Tooltip("resource amount range, " +
                                                                                            "this is used for system automatically regenerate resource")]
        public float prob;
        
        [FoldoutGroup("Gameplay/Resource"), HorizontalGroup("Gameplay/Resource/1")]
        public bool renewable;
        
        [ShowIf(nameof(renewable)),FoldoutGroup("Gameplay/Resource"), HorizontalGroup("Gameplay/Resource/2")]
        public int regenerationTimeHours;
        protected override void InitDefaults()
        {
            base.InitDefaults();
            if (baseTag == default)
                baseTag = BaseTag.Resources;
            if (factionTag == default)
                factionTag = FactionTag.Neutral;
        }
        public override int GetGeneralTypeIndex()
        {
            return (int)type;
        }

        public override int GetSubtypeIndex()
        {
            throw new ArgumentException("Resource data item has no subtypes");
        }
    }
}
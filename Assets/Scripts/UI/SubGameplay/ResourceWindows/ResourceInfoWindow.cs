using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public class ResourceInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>
    {
        public static ResourceInfoWindow Instance;


        private ResourceType _populationResourceType;

        public void UpdateStaticData(List<ResourceData> datas)
        {
            var count = datas.Count;
            _populationResourceType = World.DefaultGameObjectInjectionWorld.EntityManager
                .CreateEntityQuery(typeof(PopulationResourceType))
                .GetSingleton<PopulationResourceType>().Value;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    slot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[datas[i].resourceType];
                    slot.label.text = datas[i].resourceType.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }

        public void UpdateDynamicData(List<ResourceData> datas)
        {
            var count = datas.Count;

            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    var data = datas[i];
                    var speed = data.hoursPerUnit < 0 ? 0 : 1f / data.hoursPerUnit;

                    if (data.resourceType == _populationResourceType)
                    {
                        // Population resource : summoned unit count (+ conjuring unit count) / total storage
                        slot.value.text = $"{data.occupiedCount}(+{data.virtualOccupiedCount})/{data.storage}\n ";
                        slot.value.color = data.availableAmount <= 0 ? Color.red : Color.white;
                    }
                    else
                    {
                        var preNum = data.availableAmount;
                        slot.value.text = $"{preNum}/{data.storage}\n (+{speed:F2}/h)";
                        slot.value.color = preNum >= data.storage ? Color.red : Color.white;
                    }
                    // Population resource amount accounts for available value, not total value
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }

       

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }
    }
}
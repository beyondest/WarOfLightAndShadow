using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public class ResourceInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>
    {
        public static ResourceInfoWindow Instance;


        public void UpdateStaticData(List<ResourceData> datas)
        {
            var count = datas.Count;

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

        public void UpdateDynamicData(List<ResourceData> datas, in PopulationResourceData populationResourceData)
        {
            var count = datas.Count;

            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    var data = datas[i];
                    var speed = data.amountPerHour;

                    switch (data.resourceType)
                    {
                        case ResourceType.SoulPact:
                            // Population resource : summoned unit count (+ conjuring unit count) / total storage
                            slot.value.text =
                                $"{populationResourceData.occupiedCount}(+{populationResourceData.virtualOccupiedCount})/{populationResourceData.storage}\n ";
                            slot.value.color = data.availableAmount <= 0 ? Color.red : Color.white;
                            break;
                        
                        case ResourceType.Mana:
                        case ResourceType.Crystal:
                            var preNum = data.availableAmount;
                            slot.value.text = $"{preNum}/{data.storage}\n (+{speed:F2}/h)";
                            slot.value.color = preNum >= data.storage ? Color.red : Color.white;
                            break;
                        case ResourceType.Aetherium:
                        case ResourceType.Essence:
                            slot.value.text = $"{data.availableAmount}";
                            slot.value.color = Color.white;
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(data.resourceType);
                            break;
                    }
                  
               
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
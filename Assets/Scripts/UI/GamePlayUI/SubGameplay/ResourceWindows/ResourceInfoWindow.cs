using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
{
    public class ResourceInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>
    {
        public static ResourceInfoWindow Instance;
        public int occupiedPopulationValue;
        public int totalAmount;
        
        public void UpdateStaticData(DynamicBuffer<ResourceTypeToAvailableAmount> datas)
        {
            var count = datas.Length;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    slot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[datas[i].ResourceType];
                    slot.label.text = datas[i].ResourceType.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
        
        public void UpdateDynamicData(DynamicBuffer<ResourceTypeToAvailableAmount> datas)
        {
            var count = datas.Length;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    var data = datas[i];
                    if (data.ResourceType != ResourceType.SoulPact)
                    {
                        slot.value.text = data.Amount.ToString();
                    }
                    else
                    {
                        // Population resource amount accounts for available value, not total value
                        slot.value.text = $"{occupiedPopulationValue}/{totalAmount}";
                        slot.value.color = occupiedPopulationValue > totalAmount ? Color.red : Color.white;
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
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

  
    }
}
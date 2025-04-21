using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public class ResourceInfoWindow : UIUtils.MultiSlotsWindow<AttributeSlot>
    {


        public static ResourceInfoWindow Instance;
        public int occupiedPopulationValue;
        
        public void UpdateStaticData(DynamicBuffer<ResourceData> datas)
        {
            var count = datas.Length;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    slot.icon.sprite = BasicResourceManager.Instance.ResourceSprites[datas[i].ResourceType];
                    slot.label.text = datas[i].ResourceType.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
        
        public void UpdateDynamicData(DynamicBuffer<ResourceData> datas)
        {
            var count = datas.Length;
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < count)
                {
                    Slots[i].SetActive(true);
                    var slot = SlotComponents[i];
                    var data = datas[i];
                    if (data.ResourceType != ResourceType.Population)
                    {
                        slot.value.text = data.Amount.ToString();
                    }
                    else
                    {   
                        // Population resource amount accounts for available value, not total value
                        slot.value.text = $"{occupiedPopulationValue}/{data.Amount + occupiedPopulationValue}";
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
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }
    }
}
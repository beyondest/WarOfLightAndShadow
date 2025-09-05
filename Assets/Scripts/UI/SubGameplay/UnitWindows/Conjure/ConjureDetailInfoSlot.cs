using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public class ConjureDetailInfoSlot : UnitDetailWindow
    {
        
        public void SetCurConjureCount(int conjureCount) => _currentConjureCount = conjureCount;
        
        protected override void Awake()
        {
        }


        protected override void Start()
        {
        }

        protected override void Update()
        {
            
        }
        
        
        private int _currentConjureCount;

        public override void UpdateCostSlots()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var costList = Em.GetBuffer<CostList>(TargetEntity);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = $"x{cost.Amount * (_currentConjureCount == 0 ? 1 : _currentConjureCount)}";
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}
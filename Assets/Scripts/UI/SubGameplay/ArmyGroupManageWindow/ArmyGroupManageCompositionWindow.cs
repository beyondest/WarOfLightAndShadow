using System;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Components.SubGameplay;
using SparFlame.Core.GlobalMono;
using SparFlame.Database;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageCompositionWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupUnitCompositionSlot>
    {


        public bool TrySwitchTarget(Entity target)
        {
            ClearSelected();
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!_em.HasComponent<ArmyGroupAttr>(target))
                return false;
            _targetEntity = target;
            return true;
        }

   
   
        public void ClearSelected()
        {
            _selectedUnits.Clear();
            foreach (var slot in SlotComponents)
            {
                slot.ToggleSelected(false);
            }
        }
        public void UpDateComposition(bool tierFilterEnabled, Tier currentFilterTier, List<UnitType> currentFilterUnitTypes)
        {
            _tierFilterEnabled = tierFilterEnabled;
            _currentFilterTier = currentFilterTier;
            _currentFilterUnitTypes = currentFilterUnitTypes;
            var unitTypeDatas = _em.GetBuffer<ArmyGroupUnitTypeData>(_targetEntity);

            int i = 0;
            foreach(var unitTypeData in unitTypeDatas)
            {
                if (i >= Slots.Count) break;
                var item = DatabaseManager.UnitDatabaseSo.GetItemById(unitTypeData.PrefabId);
                if(tierFilterEnabled && currentFilterTier != item.curTier)continue;
                if(!currentFilterUnitTypes.Contains(item.type))continue;
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                slot.SetActive(true);
                slotComponent.SetTarget(unitTypeData);
                i++;
            }

            for (int index = i; index < Slots.Count; index++)
            {
                var slot = Slots[index];
                slot.SetActive(false);
            }
        }
      

        #region ButtonMethods



        public override void OnClickSlot(int slotIndex)
        {
            var slotComponent = SlotComponents[slotIndex];
            var data = slotComponent.GetData();
            if (_selectedUnits.Contains(data))
            {
                _selectedUnits.Remove(data);
                slotComponent.ToggleSelected(false);
            }
            else
            {
                _selectedUnits.Add(data);
                slotComponent.ToggleSelected(true);
            }
        }

        public void OnClickSelectAll()
        {
            var typeDatas = _em.GetBuffer<ArmyGroupUnitTypeData>(_targetEntity);
            if (_selectedUnits.Count == typeDatas.Length)
            {
                _selectedUnits.Clear();
                foreach (var slotComponent in SlotComponents)
                {
                    slotComponent.ToggleSelected(false);
                }
                return;
            }
            _selectedUnits.Clear();
            for (var i = 0; i < SlotComponents.Count; i++)
            {
                if(i >= typeDatas.Length)break;
                if(!Slots[i].activeSelf)continue;
                var slotComponent = SlotComponents[i];
                var data = slotComponent.GetData();
                _selectedUnits.Add(data);
                slotComponent.ToggleSelected(true);
            }
        }

        public void OnClickRemoveSelected()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var typeData in _selectedUnits)
            {
                var request = ecb.CreateEntity();
                ecb.AddComponent(request, new RemoveFromArmyGroupRequest
                {
                    ArmyGroup = _targetEntity,
                    RemoveType = RemoveFromArmyGroupType.MoveOutAllSameId,
                    MoveOutId = typeData.PrefabId
                });
                ecb.AddComponent<SubGameplayEntityTag>(request);
            }

            ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
            ecb.Dispose();
            _selectedUnits.Clear();
            foreach (var slotComponent in SlotComponents)
            {
                slotComponent.ToggleSelected(false);
            }
            FrameDelayInvoker.Instance.InvokeAfterFrames(1, () =>
            {
                UpDateComposition(_tierFilterEnabled, _currentFilterTier, _currentFilterUnitTypes);
            });
        }

        #endregion


        #region EventFunctions

      

        protected override void Start()
        {
            
        }

        #endregion

        private Entity _targetEntity;
        private EntityManager _em;
        private readonly List<ArmyGroupUnitTypeData> _selectedUnits = new();
        private bool _tierFilterEnabled;
        private Tier _currentFilterTier;
        private List<UnitType> _currentFilterUnitTypes = new();
        


    }
}
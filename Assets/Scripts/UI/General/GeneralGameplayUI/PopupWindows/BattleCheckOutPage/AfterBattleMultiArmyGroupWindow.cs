using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace SparFlame.UI.General.GeneralGameplayUI.PopupWindows.BattleCheckOutPage
{
    public class AfterBattleMultiArmyGroupWindow : MultiSlotWindowUtils.MultiSlotsWindow<AfterBattleMultiArmyGroupSlot>
    {
        public void UpdateCandidates(NativeArray<Entity> armyGroups)
        {
            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < armyGroups.Length)
                {
                    var armyGroup = armyGroups[i];
                    slot.SetActive(true);
                    slotComponent.SetTarget(armyGroup);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
        }
        
    }
}
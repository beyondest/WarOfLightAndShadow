using System.Collections.Generic;
using Unity.Entities;

namespace SparFlame.UI.General
{
    public class PreBattleMultiArmyGroupsWindow : MultiSlotWindowUtils.MultiSlotsWindow<PreBattleMultiArmyGroupSlot>
    {
        public void UpdateCandidates(List<ArmyGroupReachedInfo> armyGroups)
        {

            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < armyGroups.Count)
                {
                    slot.SetActive(true);
                    slotComponent.SetTarget(armyGroups[i].ArmyGroup, armyGroups[i].IfReached);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
            
        }
    }

    public struct ArmyGroupReachedInfo
    {
        public Entity ArmyGroup;
        public bool IfReached;
    }


}
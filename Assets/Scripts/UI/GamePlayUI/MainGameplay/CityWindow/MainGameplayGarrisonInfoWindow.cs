using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class MainGameplayGarrisonInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<MainGameplayGarrisonInfoSlot>,MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static MainGameplayGarrisonInfoWindow Instance { get; private set; }
        public void Awake()
        {
            Instance = this;
        }

        public bool TrySwitchTarget(Entity target)
        {
            throw new System.NotImplementedException();
        }

        public bool HasTarget()
        {
            throw new System.NotImplementedException();
        }

        public void ClearCloseUpTarget()
        {
            throw new System.NotImplementedException();
        }
    }
}
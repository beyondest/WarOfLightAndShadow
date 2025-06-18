using System;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupMulti2DWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupMulti2DSlot>,MultiSlotWindowUtils.ISingleTargetWindow
    {

        public static ArmyGroupMulti2DWindow Instance;

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
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

        public void DisableClickRoutine()
        {
            throw new NotImplementedException();
        }

        public void EnableClickRoutine()
        {
            
        }
    }
}
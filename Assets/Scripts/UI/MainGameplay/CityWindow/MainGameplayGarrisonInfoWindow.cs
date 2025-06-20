using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class MainGameplayGarrisonInfoWindow : MultiSlotWindowUtils.MultiSlotsWindow<MainGameplayGarrisonInfoSlot>,MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static MainGameplayGarrisonInfoWindow Instance;
        

        public bool TrySwitchTarget(Entity target)
        {
            return false;
        }

        public bool HasTarget()
        {
            return _targetEntity == Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        private Entity _targetEntity = Entity.Null;

        #region EventFunctions

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            Hide();
        }

        #endregion
    }
}
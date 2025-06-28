using SparFlame.Components.General;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageDetailInfoSlot : ArmyGroupDetailWindow
    {
        private EntityQuery _subGamingTag;

        protected override void Awake()
        {
            
        }

        protected override void Start()
        {
            _subGamingTag = Em.CreateEntityQuery(typeof(SubGamingTag));
        }
        private void Update()
        {
            if(!IsOpened() || !HasTarget()
                           || _subGamingTag.IsEmpty)return;
            UpdateDynamicData();
            
        }
    }
}
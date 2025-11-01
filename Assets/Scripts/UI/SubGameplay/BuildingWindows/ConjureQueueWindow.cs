using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.SubGameplay
{
    public class ConjureQueueWindow : MultiSlotWindowUtils.MultiSlotsWindow<ConjureQueueSlot>,MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static ConjureQueueWindow Instance;
        
        public bool TrySwitchTarget(Entity target)
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if(!_em.HasComponent<ConjureAttr>(target))return false;
            _targetEntity = target;
            UpdateDynamicData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;   
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }


        public override void OnClickSlot(int slotIndex)
        {
        }
        // Internal Data
        
        private EntityManager _em;
        private Entity _targetEntity;
        private EntityQuery _gamingTag;

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
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            Hide();
        }

        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if(!IsOpened())return;
            if(_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<SubGameplayGeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }
            UpdateDynamicData();
        }

        private void OnDestroy()
        {
            try
            {
                if(_gamingTag != default)
                    _gamingTag.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }


        private void UpdateDynamicData()
        {
            var conjureAttribute = _em.GetComponentData<ConjureAttr>(_targetEntity);
            // Update conjure button look
         
            // Update conjuring queue
            var conjuringDatas = _em.GetBuffer<ConjuringData>(_targetEntity);
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < conjuringDatas.Length)
                {
                    Slots[i].SetActive(true);
                    var slotComponent = SlotComponents[i];
                    var conjureData = conjuringDatas[i];
                    var subGameplayGeneralAttr = _em.GetComponentData<PrefabId>(conjureData.ConjuringEntity);
                    var info = UnitWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(conjureAttribute.ConjuringType,
                        subGameplayGeneralAttr.value);
                    slotComponent.unitNameText.text = info.GameplayName;
                    slotComponent.button!.image.sprite = info.Sprite;
                    slotComponent.remainedCountText.text =
                        $"{conjureData.ConjuredAmount} / {conjureData.TargetAmount}";
                    slotComponent.remainedTimeText.text =
                        UIMathMethods.FormatTimeFromHours((int)conjureData.ThisTaskRemainingTime);
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }

        }

  
    }
}
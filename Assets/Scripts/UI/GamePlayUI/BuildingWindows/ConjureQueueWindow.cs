using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.GamePlay
{
    public class ConjureQueueWindow : UIUtils.MultiSlotsWindow<ConjureQueueSlot>,UIUtils.ISingleTargetWindow
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

        
        
        public override void OnClickSlot(int slotIndex)
        {
            // TODO : Cut the conjuring queue
        }


        // Internal Data
        
        private EntityManager _em;
        private Entity _targetEntity;
        private EntityQuery _notPauseTag;

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            Hide();
        }

        private void Update()
        {
            if(!IsOpened())return;
            if (_notPauseTag.IsEmpty) return;
            if(_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<GeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }
            UpdateDynamicData();
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
                    var generalAttr = _em.GetComponentData<GeneralAttr>(conjureData.ConjuringEntity);
                    var info = UnitWindowResourceManager.Instance.GetInfo(conjureAttribute.ConjuringType,
                        generalAttr.ID);
                    slotComponent.unitNameText.text = info.GameplayName;
                    slotComponent.button!.image.sprite = info.Sprite;
                    slotComponent.remainedCountText.text =
                        $"{conjureData.ConjuredAmount} / {conjureData.TargetAmount}";
                    slotComponent.remainedTimeText.text =
                        UIMathMethods.FormatTime((int)conjureData.RemainingTimeSeconds);
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }

        }

  
    }
}
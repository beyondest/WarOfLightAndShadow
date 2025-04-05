using System.Globalization;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Movement;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Units;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class UnitDetailWindow : UIUtils.MultiSlotsWindow<AttributeSlot>,UIUtils.ISingleTargetWindow
    {
        // Config
        [Header("Custom Config")] 
        [SerializeField]
        private TMP_Text unitType;
        [SerializeField]
        private TMP_Text unitHp;
        [SerializeField]
        private TMP_Text unitMoveSpeed;
        [SerializeField]
        private Image unitIcon;
        [SerializeField]
        private Image hpIcon;


        // Interface
        public static UnitDetailWindow Instance;

        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<UnitAttr>(target)
                || !_em.HasComponent<MovableData>(target)
                || !_em.HasBuffer<CostList>(target)
                || !_em.HasComponent<StatData>(target))
                return false;
            _targetEntity = target;
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

    
        
        // Internal Data
        private AsyncOperationHandle<GameObject> _costSlotPrefabHandle;
        private Entity _targetEntity = Entity.Null;
        
        
        

        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        #region EventFunction


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
            panel.SetActive(false);
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            
            if (!UnitWindowResourceManager.Instance.IsResourceLoaded() 
                ||!BasicWindowResourceManager.Instance.IsResourceLoaded()) return;
            if (_targetEntity != Entity.Null)
                UpdateUnitDetailInfo();
        }

        #endregion


        private void UpdateUnitDetailInfo()
        {
            var interactableAttr = _em.GetComponentData<InteractableAttr>(_targetEntity);
            var attr = _em.GetComponentData<UnitAttr>(_targetEntity);
            var movableData = _em.GetComponentData<MovableData>(_targetEntity);
            var statData = _em.GetComponentData<StatData>(_targetEntity);
            var costList = _em.GetBuffer<CostList>(_targetEntity);
            // Visualize these attributes
            unitType.text = attr.Type.ToString();
            unitMoveSpeed.text = movableData.MoveSpeed.ToString(CultureInfo.InvariantCulture);
            unitHp.text = statData.CurValue.ToString(CultureInfo.InvariantCulture) + "/" +
                          statData.MaxValue.ToString(CultureInfo.InvariantCulture);
            unitIcon.sprite = UnitWindowResourceManager.Instance.UnitSprites[attr.Type];
            hpIcon.sprite = BasicWindowResourceManager.Instance.FactionHpSprites[interactableAttr.FactionTag];
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicWindowResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = cost.Amount.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }


}
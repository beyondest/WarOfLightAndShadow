using System;
using System.Globalization;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Plastic.Newtonsoft.Json.Serialization;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class BuildingDetailWindow : UIUtils.MultiSlotsWindow<AttributeSlot>,UIUtils.ISingleTargetWindow
    {
        
        // Config
        [Header("Custom Config")]
        [SerializeField]
        private TMP_Text buildingType;
        [SerializeField]
        private TMP_Text buildingHp;
        [SerializeField]
        private Image buildingIcon;
        [SerializeField]
        private Image buildingHpIcon;
        
        // Interface
        public static BuildingDetailWindow Instance;
        public Action<Entity> EcsGhostShowTarget;
        [NonSerialized] public bool InitWindowEvents = false;
        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }
 
        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<BuildingAttr>(target)
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

        #region ButtonMethods
        public void OnClickRelocate()
        {
            if (_buildingAttr.State != BuildingState.Idle)
            {
                Debug.Log("Not in idle state, cannot enter building movement state");
                return;
            }
            if(!ConstructWindow.Instance.IsOpened())
                ConstructWindow.Instance.OnClickConstructEnter();
            EcsGhostShowTarget?.Invoke(_targetEntity);
        }

        public void OnClickStore()
        {
            throw new NotImplementedException();
        }

        public void OnClickRecycle()
        {
            throw new NotImplementedException();
        }
        #endregion

        // Internal Data
        private AsyncOperationHandle<GameObject> _costSlotPrefabHandle;
        private Entity _targetEntity = Entity.Null;
        
        // Cache
        private GameObject _costSlotPrefab;
        private BuildingAttr _buildingAttr;
        
        
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
            
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded()
                ||!BasicWindowResourceManager.Instance.IsResourceLoaded()) return;
            if (_targetEntity != Entity.Null)
                UpdateBuildingDetailInfo();
        }
        #endregion
        
        private void UpdateBuildingDetailInfo()
        {
            var interactableAttr = _em.GetComponentData<InteractableAttr>(_targetEntity);
            _buildingAttr = _em.GetComponentData<BuildingAttr>(_targetEntity);
            var statData = _em.GetComponentData<StatData>(_targetEntity);
            var costList = _em.GetBuffer<CostList>(_targetEntity);
            // Visualize these attributes
            buildingType.text = _buildingAttr.Type.ToString();
            buildingHp.text = statData.CurValue.ToString(CultureInfo.InvariantCulture) + "/" +
                          statData.MaxValue.ToString(CultureInfo.InvariantCulture);
            buildingIcon.sprite = BuildingWindowResourceManager.Instance.BuildingTypeSprites[_buildingAttr.Type];
            buildingHpIcon.sprite = BasicWindowResourceManager.Instance.FactionHpSprites[interactableAttr.FactionTag];
            
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
using System;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Generate;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class BuildingDetailWindow : UIUtils.MultiSlotsWindow<AttributeSlot>, UIUtils.ISingleTargetWindow
    {
        // Config
        [Header("Custom Config")] [SerializeField]
        private TMP_Text buildingType;

        [SerializeField] private Image buildingTypeIcon;

        [SerializeField] private Image functionIcon;
        [SerializeField] private TMP_Text functionNameText;

        [SerializeField] private TMP_Text doingThingsRemainedTimeText;
        [SerializeField] private Image doingThingsIcon;
        [SerializeField] private TMP_Text doingThingsDescriptionText;
        [SerializeField] private TMP_Text doingLabelText;
        [SerializeField] private TMP_Text thingsLabelText;
        [SerializeField] private AssetReferenceSprite buildingIdleIcon;

        // Interface
        public static BuildingDetailWindow Instance;
        public Action<Entity> EcsGhostShowTarget;
        [NonSerialized] public bool InitWindowEvents = false;

        // Cache
        private Sprite _idleSprite;
        
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

            if (!ConstructWindow.Instance.IsOpened())
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
        private Entity _targetEntity = Entity.Null;
        private AsyncOperationHandle<Sprite> _spriteHandle;

        // Cache
        private GameObject _costSlotPrefab;
        private BuildingAttr _buildingAttr;


        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        #region EventFunction

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _spriteHandle = CR.LoadAssetRefAsync<Sprite>(buildingIdleIcon, sprite =>
            {
                _idleSprite = sprite;
            });
        }

 

        protected override void OnDisable()
        {
            base.OnDisable();
            Addressables.Release(_spriteHandle);
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
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded()
                || !BasicWindowResourceManager.Instance.IsResourceLoaded()
                || !IsResourceLoaded()) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<InteractableAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            UpdateBuildingDetailInfo();
        }

        #endregion

        protected override bool IsResourceLoaded()
        {
            return base.IsResourceLoaded() && _spriteHandle.IsValid() && _spriteHandle.IsDone;
        }
        
        private void UpdateBuildingDetailInfo()
        {
            var interactableAttr = _em.GetComponentData<InteractableAttr>(_targetEntity);
            _buildingAttr = _em.GetComponentData<BuildingAttr>(_targetEntity);
            var costList = _em.GetBuffer<CostList>(_targetEntity);
            
            // Visualize type attributes
            buildingType.text = _buildingAttr.Type.ToString();
            buildingTypeIcon.sprite = BuildingWindowResourceManager.Instance.BuildingTypeSprites[_buildingAttr.Type];
            
            // Visualize cost attributes
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicWindowResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = $"x{cost.Amount}";
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
            // Visualize function panel and doingThings panel
            if (_buildingAttr.State != BuildingState.Working)
            {
                doingThingsIcon.sprite = _idleSprite;
                doingThingsRemainedTimeText.text = "";
                doingThingsDescriptionText.text = "";
                thingsLabelText.text = "";
                doingLabelText.text = "Idle";
            }
            switch (_buildingAttr.Type)
            {
                case BuildingType.ConjuringShrines:
                {
                    var conjureAttribute = _em.GetComponentData<ConjureAttr>(_targetEntity);
                    functionIcon.enabled = true;
                    functionIcon.sprite =
                        BuildingWindowResourceManager.Instance.FunctionConjuringButtonSprites[interactableAttr.Tier];
                    functionNameText.enabled = true;
                    functionNameText.text = "Conjure";
                    if (_buildingAttr.State == BuildingState.Working)
                    {
                        doingThingsIcon.sprite =
                            UnitWindowResourceManager.Instance.UnitSprites[conjureAttribute.ConjuringType];
                        doingThingsRemainedTimeText.text = UIMathMethods.FormatTime(conjureAttribute.RemainingTime);
                        doingThingsDescriptionText.text =
                            $"{conjureAttribute.ConjuredAmount} / {conjureAttribute.TargetAmount}";
                        doingLabelText.text = "Conjuring";
                        thingsLabelText.text = conjureAttribute.ConjuringType.ToString();
                    }
                    break;
                }
                case BuildingType.Generators:
                {
                    functionIcon.enabled = true;
                    var generateAttribute = _em.GetComponentData<GenerateAttr>(_targetEntity);
                    functionIcon.sprite =
                        BuildingWindowResourceManager.Instance.FunctionGeneratingButtonSprites[interactableAttr.Tier];
                    functionNameText.enabled = true;
                    functionNameText.text = "Generate";
                    if (_buildingAttr.State == BuildingState.Working)
                    {
                        doingThingsIcon.sprite =
                            BasicWindowResourceManager.Instance.ResourceSprites[generateAttribute.GenerateResourceType];
                        doingThingsRemainedTimeText.text = UIMathMethods.FormatTime(generateAttribute.RemainingTime);
                        doingThingsDescriptionText.text =
                            $"{generateAttribute.GeneratedAmount} / {generateAttribute.TargetAmount}";
                        doingLabelText.text = "Generating";
                        thingsLabelText.text = generateAttribute.GenerateResourceType.ToString();
                    }
                    break;
                }
                case BuildingType.Fortifications:
                case BuildingType.Dwellings:
                case BuildingType.Ornaments:
                    functionIcon.enabled = false;
                    functionNameText.enabled = false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
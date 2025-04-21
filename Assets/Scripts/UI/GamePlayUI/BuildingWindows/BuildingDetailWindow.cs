using System;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Exp;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Generate;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.GamePlaySystem.Spawn;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class BuildingDetailWindow : UIUtils.MultiSlotsWindow<AttributeSlot>, UIUtils.ISingleTargetWindow
    {
        // Config
        [Header("General Information")] [SerializeField]
        private bool showCostSlots;

        [SerializeField] private bool isMainInfoSingleton;

        [SerializeField] private TMP_Text generalTypeText;
        [SerializeField] private Image generalTypeIcon;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image idSingleIcon;
        [SerializeField] private Image interactAbilityTriangle;

        [Header("Building detail")] [SerializeField]
        private Image buildingStateIcon;

        [SerializeField] private TMP_Text buildingStateText;

        [Header("Garrison Panel")]
        [SerializeField]
        private GameObject garrisonInfoPanel;

        [SerializeField] private TMP_Text garrisonCountText;
        
        [Header("Generate panel")] [SerializeField]
        private GameObject generatePanel;

        [SerializeField] private Image generateResourceIcon;
        [SerializeField] private TMP_Text generateTypeText;
        [SerializeField] private TMP_Text generateSpeedText;
        [SerializeField] private TMP_Text generateMinRequireUnitsText;
        
        [Header("Conjure panel")] [SerializeField]
        private GameObject conjurePanel;

        [SerializeField] private Image conjureButtonIcon;
        [SerializeField] private TMP_Text conjureTypeNameText;

        [Header("Dwelling panel")]
        [SerializeField]
        private GameObject dwellingPanel;
        [SerializeField] private TMP_Text dwellingCountText;
        [SerializeField] private Image dwellingResourceIcon;
        [Header("Ornament panel")]
        [SerializeField] private GameObject ornamentPanel;
        [SerializeField] private Image ornamentBuffImage;
        [SerializeField] private TMP_Text ornamentBuffDescriptionText;
        
        // Interface
        public static BuildingDetailWindow Instance;
        public Action<Entity> EcsGhostShowTarget;

        [NonSerialized] public bool InitConstructEvents = false;


        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }

        public bool TrySwitchTarget(Entity target)
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!Em.HasComponent<BuildingAttr>(target))
                return false;
            _targetEntity = target;
            UpdateStaticData();
            // UpdateDynamicData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        #region ButtonMethods

        public void OnClickConjureButton()
        {
            if (!ConjureWindow.Instance.IsOpened())
                ConjureWindow.Instance.Show();
            ConjureWindow.Instance.TrySwitchTarget(_targetEntity);
        }

        public void OnClickRelocate()
        {
            if (Em.HasComponent<OocTag>(_targetEntity) ||
                Em.HasComponent<ConstructingTag>(_targetEntity)
                ||_hasGarrisonUnits)
            {
                // TODO : Hints pop support
                Debug.Log(" not allow to relocate, this should pop up hints");
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
        private bool _hasGarrisonUnits;
        
        
        // Cache
        private GameObject _costSlotPrefab;
        private BuildingAttr _buildingAttr;

        // ECS
        protected EntityManager Em;
        private EntityQuery _notPauseTag;

        #region EventFunction

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void OnEnable()
        {
            if (showCostSlots)
                base.OnEnable();
            else
            {
                SetInitialized();
            }
            generatePanel.SetActive(false);
            dwellingPanel.SetActive(false);
            ornamentPanel.SetActive(false);
            conjurePanel.SetActive(false);
            interactAbilityTriangle.enabled = false;
            
        }

        protected override void OnDisable()
        {
            if (showCostSlots)
                base.OnDisable();
        }

        protected virtual void Start()
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = Em.CreateEntityQuery(typeof(NotPauseTag));
        }

        protected virtual void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!BuildingWindowResourceManager.Instance.IsResourceLoaded()
                || !BasicResourceManager.Instance.IsResourceLoaded()) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!Em.HasComponent<GeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            UpdateDynamicData();
        }

        #endregion


        private void UpdateStaticData()
        {
            var generalAttr = Em.GetComponentData<GeneralAttr>(_targetEntity);
            var dataItem = DatabaseManager.BuildingDatabaseSo.GetItemById(generalAttr.ID);
            _buildingAttr = Em.GetComponentData<BuildingAttr>(_targetEntity);
            description.text = dataItem.description;
            // Visualize type attributes
            generalTypeText.text = _buildingAttr.Type.ToString();
            generalTypeIcon.sprite =
                BuildingWindowResourceManager.Instance.BuildingGeneralTypeSprites[_buildingAttr.Type];
            idSingleIcon.sprite = BuildingWindowResourceManager.Instance
                .GetInfo(_buildingAttr.Type, generalAttr.ID).Sprite;
            if (showCostSlots)
                VisualizeCostSlots();
            
            
            interactAbilityTriangle.enabled = false; // fortification panel
            generatePanel.SetActive(false);
            conjurePanel.SetActive(false);
            dwellingPanel.SetActive(false);
            ornamentPanel.SetActive(false);
            
            if (Em.HasComponent<GarrisonAttr>(_targetEntity))
            {
                var garrisonAttr = Em.GetComponentData<GarrisonAttr>(_targetEntity);
                garrisonInfoPanel.SetActive(true);
                if (!isMainInfoSingleton) garrisonCountText.text = $"{garrisonAttr.MaxGarrisonCount}";
            }
            
            switch (_buildingAttr.Type)
            {
                case BuildingType.Generators:
                    generatePanel.SetActive(true);
                    var generateAttribute = Em.GetComponentData<GenerateAttr>(_targetEntity);
                    generateResourceIcon.sprite =
                        BasicResourceManager.Instance.ResourceSprites[generateAttribute.GenerateResourceType];
                    generateTypeText.text = generateAttribute.GenerateResourceType.ToString();
                    generateMinRequireUnitsText.text = generateAttribute.MinCultivatorsRequireToGenerate.ToString();
                    if (!isMainInfoSingleton)
                        generateSpeedText.text = $"{generateAttribute.MinCultivatorsRequireToGenerate}/s";
                    break;
                case BuildingType.Fortifications:
                    interactAbilityTriangle.enabled = true;
                    break;
                case BuildingType.ConjuringShrines:
                    conjurePanel.SetActive(true);
                    var conjureAttribute = Em.GetComponentData<ConjureAttr>(_targetEntity);
                    var currentTier = Em.GetComponentData<ExpData>(_targetEntity).CurTier;
                    // Main building window then set to conjure button icon, building slot then set to unitType icon
                    conjureButtonIcon.sprite = isMainInfoSingleton
                        ? BuildingWindowResourceManager.Instance.FunctionConjuringButtonSprites[currentTier]
                        : UnitWindowResourceManager.Instance.UnitGeneralTypeSprites[conjureAttribute.ConjuringType];
                    conjureTypeNameText.text = conjureAttribute.ConjuringType + "Conjuration";
                    break;
                case BuildingType.Dwellings:
                    dwellingPanel.SetActive(true);
                    var dwellingAttr = Em.GetComponentData<DwellingAttr>(_targetEntity);
                    dwellingCountText.text = dwellingAttr.Amount.ToString();
                    dwellingResourceIcon.sprite =
                        BasicResourceManager.Instance.ResourceSprites[dwellingAttr.ResourceType];
                    
                    break;
                case BuildingType.Ornaments:
                    if(Em.HasComponent<AttackAbility>(_targetEntity)
                       || Em.HasComponent<HealAbility>(_targetEntity)
                       || Em.HasComponent<HarvestAbility>(_targetEntity))
                        interactAbilityTriangle.enabled = true;
                    if (Em.HasComponent<StaticBuffAttr>(_targetEntity))
                    {
                        var staticBuffAttr = Em.GetComponentData<StaticBuffAttr>(_targetEntity); 
                        ornamentPanel.SetActive(true);
                        ornamentBuffImage.sprite = BasicResourceManager.Instance.BuffSprites[staticBuffAttr.Type];
                        ornamentBuffDescriptionText.text = "Not implemented";
                    }
                    
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Check should open garrison window or conjure queue window
            if (isMainInfoSingleton)
                ShouldOpenGarrisonInfoConjureQueueAndMiniConjure();
        }

        private void ShouldOpenGarrisonInfoConjureQueueAndMiniConjure()
        {
            if (GarrisonInfoWindow.Instance.TrySwitchTarget(_targetEntity))
            {
                if( !GarrisonInfoWindow.Instance.IsOpened())
                    GarrisonInfoWindow.Instance.Show();
            }
            else GarrisonInfoWindow.Instance.Hide();

            if (ConjureQueueWindow.Instance.TrySwitchTarget(_targetEntity)
               )
            {
                if(!ConjureQueueWindow.Instance.IsOpened())
                    ConjureQueueWindow.Instance.Show();
            }
            else
                ConjureQueueWindow.Instance.Hide();

            if (MiniConjureWindow.Instance.TrySwitchTarget(_targetEntity)
               )
            {
                if(!MiniConjureWindow.Instance.IsOpened())
                    MiniConjureWindow.Instance.Show();
            }
            else MiniConjureWindow.Instance.Hide();
        }

        private void UpdateDynamicData()
        {
            _buildingAttr = Em.GetComponentData<BuildingAttr>(_targetEntity);
            // Visualize function panel and doingThings panel
            var underAttack = Em.HasComponent<OocTag>(_targetEntity);
            var constructing = Em.HasComponent<ConstructingTag>(_targetEntity);
            var generating = Em.HasComponent<GeneratingTag>(_targetEntity);
            var conjuring = Em.HasComponent<ConjuringTag>(_targetEntity);
            var currentState = BuildingUtils.GetBuildingState(underAttack, constructing, conjuring, generating);
            buildingStateIcon.sprite =
                BuildingWindowResourceManager.Instance.BuildingStateSprites[currentState];
            buildingStateText.text = currentState.ToString();
            
            // Check garrison data
            _hasGarrisonUnits = false;
            if (Em.HasComponent<GarrisonAttr>(_targetEntity))
            {
                var garrisonAttr = Em.GetComponentData<GarrisonAttr>(_targetEntity);
                var entities = Em.GetBuffer<GarrisonEntity>(_targetEntity);
                garrisonCountText.text = $"{entities.Length} / {garrisonAttr.MaxGarrisonCount}";
                if (entities.Length > 0) _hasGarrisonUnits = true;
            }
            
            switch (_buildingAttr.Type)
            {
                case BuildingType.Generators:
                {
                    var generateAttribute = Em.GetComponentData<GenerateAttr>(_targetEntity);
                    generateSpeedText.text = $"Current : {generateAttribute.CurGenerateSpeed}/s" + "\n" +
                                             $"Max : {generateAttribute.MaxGenerateSpeed}/s";
                    break;
                }
                case BuildingType.ConjuringShrines:
                case BuildingType.Fortifications:
                case BuildingType.Dwellings:
                case BuildingType.Ornaments:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void VisualizeCostSlots()
        {
            var costList = Em.GetBuffer<CostList>(_targetEntity);
            // Visualize cost attributes
            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < costList.Length)
                {
                    Slots[i].SetActive(true);
                    var cost = costList[i];
                    var costSlot = SlotComponents[i];
                    costSlot.icon.sprite = BasicResourceManager.Instance.ResourceSprites[cost.Type];
                    costSlot.label.text = cost.Type.ToString();
                    costSlot.value.text = $"x{cost.Amount}";
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}
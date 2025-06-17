using System;
using System.Collections.Generic;
using SparFlame.Database;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Conjure;
using SparFlame.GamePlaySystem.Garrison;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Generate;
using SparFlame.GamePlaySystem.Hints;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Ooc;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay
{
    public class BuildingDetailWindow : MultiSlotWindowUtils.MultiSlotsWindow<AttributeSlot>, MultiSlotWindowUtils.ISingleTargetWindow
    {
        // Config
        [Header("General Information")] [SerializeField]
        private bool isMainInfoSingleton;

        [SerializeField] private TMP_Text generalTypeText;
        [SerializeField] private Image generalTypeIcon;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image idSingleIcon;
        [SerializeField] private GameObject interactAbilityPanel;

        [Header("Building detail")] [SerializeField]
        private Image buildingStateIcon;

        [SerializeField] private GameObject constructingPanel;
        [SerializeField] private TMP_Text constructTimeText;

        [SerializeField] private TMP_Text buildingStateText;

        [Header("Garrison Panel")] [SerializeField]
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

        [Header("Dwelling panel")] [SerializeField]
        private GameObject dwellingPanel;

        [SerializeField] private TMP_Text dwellingCountText;
        [SerializeField] private Image dwellingResourceIcon;

        [Header("Ornament panel")] [SerializeField]
        private GameObject ornamentPanel;

        [SerializeField] private Image ornamentBuffImage;
        [SerializeField] private TMP_Text ornamentBuffDescriptionText;
        [Header("Construct Panel")]
        [SerializeField] private GameObject constructPanel;
        [SerializeField] private GameObject upgradeButton;
        
        [Header("Upgrade Panel")]
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Image nextTierImage;
        [SerializeField] private Color notUpgradableColor = Color.red;
        
        // Interface
        public static BuildingDetailWindow Instance;
        public Action<Entity> EcsGhostShowTarget;
        public Action<Entity> EcsRecycleTarget;
        public Action<Entity> EcsGetExpStaticConfig;
        
        public override void Hide()
        {
            base.Hide();
            _targetEntity = Entity.Null;
        }

        public void UpDatePlayerGlobalResourceData(DynamicBuffer<ResourceTypeToAvailableAmount> playerResources)
        {
            foreach (var costList in playerResources)
            {
                _playerResources[costList.ResourceType] = costList.Amount;
            }
        }

        public bool TrySwitchTarget(Entity target)
        {
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (!Em.HasComponent<BuildingAttr>(target))
                return false;
            _targetEntity = target;
            _ifConstructing = false;
            UpdateStaticData();
            // UpdateDynamicData();
            return true;
        }

        public void HideConstructPanel()
        {
            constructPanel.SetActive(false);
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }
        public override void LoadResources()
        {
            base.LoadResources();
            generatePanel.SetActive(false);
            dwellingPanel.SetActive(false);
            ornamentPanel.SetActive(false);
            conjurePanel.SetActive(false);
            interactAbilityPanel.SetActive(false);
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
            var isCrystal = _buildingAttr is
                { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal };
            var isConstructing = Em.HasComponent<ConstructingData>(_targetEntity);
            var isUnderAttack = Em.IsComponentEnabled<OocTag>(_targetEntity);
            if (isUnderAttack ||
                isConstructing
                || isCrystal || _hasGarrisonUnits
                )
            {
                var hintName = HintName.None;
                if (isCrystal)
                {
                    hintName = HintName.CrystalCannotRelocate;
                }
                else if (_hasGarrisonUnits)
                {
                    hintName = HintName.CannotRelocateWhenHasGarrisonUnits;
                }
                else if (isUnderAttack) hintName = HintName.CannotRelocateWhenUnderAttack;
                else if (isConstructing) hintName = HintName.CannotRelocateWhenConstructing;
                var hintRequest = Em.CreateEntity();
                Em.AddComponent<HintRequest>(hintRequest);
                Em.SetComponentData(hintRequest, new HintRequest
                {
                    Name = hintName
                });
                return;
            }
            
            if (!ConstructWindow.Instance.IsOpened())
                ConstructWindow.Instance.EnterConstruct();
            EcsGhostShowTarget?.Invoke(_targetEntity);
        }

        

        public void OnClickRecycle()
        {
            var isCrystal = _buildingAttr is
                { Type: BuildingType.Ornaments, SubTypeIndex: (int)OrnamentType.Crystal };
            var isUnderAttack = Em.IsComponentEnabled<OocTag>(_targetEntity);
            var isConstructing = Em.HasComponent<ConstructingData>(_targetEntity);

            if (isCrystal || isUnderAttack || isConstructing)
            {
                HintName hintName;
                if (isCrystal)
                {
                    hintName = HintName.CrystalCannotRecycle;
                }
                else if (isUnderAttack) hintName = HintName.CannotRecycleWhenUnderAttack;
                else hintName = HintName.CannotRecycleWhenConstructing;
                var hintRequest = Em.CreateEntity();
                Em.AddComponent<HintRequest>(hintRequest);
                Em.SetComponentData(hintRequest, new HintRequest
                {
                    Name = hintName
                });
                return;
            }
            EcsRecycleTarget?.Invoke(_targetEntity);
        }

        public void OnClickUpgrade()
        {
            EcsGetExpStaticConfig?.Invoke(_targetEntity);
            var list = CalculateUpgradeCostList();
            var oriInfo = BuildingWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(_buildingAttr.Type,
                Em.GetComponentData<SubGameplayGeneralAttr>(_targetEntity).ID);
            var upGradeInfo = BuildingWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(_buildingAttr.Type,
                Em.GetComponentData<SubGameplayGeneralAttr>(ExpStaticConfig.NextTierPrefab).ID);
            BuildingUpgradePopUpWindow.Instance.PopUp(list,oriInfo,upGradeInfo,_targetEntity);
        }

        #endregion

        // Internal Data
        private Entity _targetEntity = Entity.Null;
        private bool _hasGarrisonUnits;
        private bool _ifConstructing;
        private FactionTag _playerFaction;

        // Cache
        private GameObject _costSlotPrefab;
        private BuildingAttr _buildingAttr;
        private readonly Dictionary<ResourceType, int> _playerResources = new();
        public ExpStaticConfig ExpStaticConfig;

        // ECS
        protected EntityManager Em;
        private EntityQuery _gamingTag;
        private EntityQuery _playerFactionQuery;

        #region EventFunction

        protected virtual void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        
       
        protected override void Start()
        {
            base.Start();
            Em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _gamingTag = Em.CreateEntityQuery(typeof(SubGamingTag));
            _playerFactionQuery = Em.CreateEntityQuery(typeof(PlayerFactionData));
        }
        
        protected virtual void Update()
        {
            if (_gamingTag.IsEmpty || _playerFactionQuery.IsEmpty) return;
            _playerFaction = _playerFactionQuery.GetSingleton<PlayerFactionData>().Value;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!Em.HasComponent<SubGameplayGeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }
            UpdateDynamicData();
        }

        #endregion


        private void UpdateStaticData()
        {
            var generalAttr = Em.GetComponentData<SubGameplayGeneralAttr>(_targetEntity);
            var dataItem = DatabaseManager.BuildingDatabaseSo.GetItemById(generalAttr.ID);
            _buildingAttr = Em.GetComponentData<BuildingAttr>(_targetEntity);
            description.text = dataItem.description;
            // Visualize type attributes
            generalTypeText.text = _buildingAttr.Type.ToString();
            generalTypeIcon.sprite =
                BuildingWindowResourceManager.Instance.BuildingGeneralTypeSprites[_buildingAttr.Type];
            idSingleIcon.sprite = BuildingWindowResourceManager.Instance
                .GetInfoByGeneralTypeAndIdx(_buildingAttr.Type, generalAttr.ID).Sprite;
            
            

            interactAbilityPanel.SetActive(false);
            generatePanel.SetActive(false);
            conjurePanel.SetActive(false);
            dwellingPanel.SetActive(false);
            ornamentPanel.SetActive(false);
            upgradePanel.SetActive(false);
            constructingPanel.SetActive(false);
            
            if (Em.HasComponent<ConstructingData>(_targetEntity))
            {
                constructingPanel.SetActive(true);
                var time = Em.GetComponentData<ConstructingData>(_targetEntity).LastTime;
                constructTimeText.text = UIMathMethods.FormatTime((int)time);
                _ifConstructing = true;
                foreach (var slot in Slots)
                {
                    slot.SetActive(false);
                }
                return;
            }
            if (multiSlotEnabled)
                VisualizeCostSlots();
            ActiveNecessaryPanels(generalAttr);
        }

        private void ActiveNecessaryPanels(SubGameplayGeneralAttr subGameplayGeneralAttr)
        {
            
            if (Em.HasComponent<GarrisonAttr>(_targetEntity))
            {
                var garrisonAttr = Em.GetComponentData<GarrisonAttr>(_targetEntity);
                garrisonInfoPanel.SetActive(true);
                if (!isMainInfoSingleton) garrisonCountText.text = $"{garrisonAttr.MaxGarrisonCount}";
            }

            if (Em.HasComponent<ExpData>(_targetEntity))
            {
                EcsGetExpStaticConfig?.Invoke(_targetEntity);
                var prefab = ExpStaticConfig.NextTierPrefab;
                if (prefab != Entity.Null)
                {
                    upgradePanel.SetActive(true);
                    var nextTierGeneralAttr = Em.GetComponentData<SubGameplayGeneralAttr>(prefab);
                    var buildingAttr = Em.GetComponentData<BuildingAttr>(prefab);
                    nextTierImage.sprite = BuildingWindowResourceManager.Instance.GetInfoByGeneralTypeAndIdx(buildingAttr.Type,
                        nextTierGeneralAttr.ID).Sprite;
                }
            }
            
            switch (_buildingAttr.Type)
            {
                case BuildingType.Generators:
                    generatePanel.SetActive(true);
                    var generateResourceType = ResourceType.SoulPact;
                    var generateSpeed = 0f;
                    var minRequiredUnit = 0;
                    
                    if (_buildingAttr.SubTypeIndex == (int)GeneratorType.PlantGenerator)
                    {
                        var plantAttr = Em.GetComponentData<PlantGenerateAttr>(_targetEntity);
                        generateResourceType = plantAttr.GenerateResourceType;
                        generateSpeed = plantAttr.GenerateSpeed;
                    }
                    else if (_buildingAttr.SubTypeIndex == (int)GeneratorType.ResourceMine)
                    {
                        var resourceMineAttr = Em.GetComponentData<ResourceMineGenerateAttr>(_targetEntity);
                        generateResourceType = resourceMineAttr.GenerateResourceType;
                        generateSpeed = resourceMineAttr.CurGenerateSpeed;
                        minRequiredUnit = resourceMineAttr.MinCultivatorsRequireToGenerate;
                    }
                    generateResourceIcon.sprite =
                        BasicUIResourceManager.Instance.ResourceSprites[generateResourceType];
                    generateTypeText.text = generateResourceType.ToString();
                    generateMinRequireUnitsText.text = minRequiredUnit.ToString();
                    if (!isMainInfoSingleton)
                        generateSpeedText.text = $"{generateSpeed}";
                    break;
                case BuildingType.Fortifications:
                    interactAbilityPanel.SetActive(true);

                    break;
                case BuildingType.ConjuringShrines:
                    if (subGameplayGeneralAttr.FactionTag != _playerFaction) break;
                    conjurePanel.SetActive(true);
                    var conjureAttribute = Em.GetComponentData<ConjureAttr>(_targetEntity);
                    var currentTier = Em.GetComponentData<ExpData>(_targetEntity).curTier;
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
                        BasicUIResourceManager.Instance.ResourceSprites[dwellingAttr.ResourceType];

                    break;
                case BuildingType.Ornaments:
                    if (Em.HasComponent<AttackAbility>(_targetEntity)
                        || Em.HasComponent<HealAbility>(_targetEntity)
                        || Em.HasComponent<HarvestAbility>(_targetEntity))
                        interactAbilityPanel.SetActive(true);
                    // if (Em.HasComponent<StaticBuffAttr>(_targetEntity))
                    // {
                    //     var staticBuffAttr = Em.GetComponentData<StaticBuffAttr>(_targetEntity);
                    //     ornamentPanel.SetActive(true);
                    //     // ornamentBuffImage.sprite = BasicUIResourceManager.Instance.BuffSprites[staticBuffAttr.Type];
                    //     ornamentBuffDescriptionText.text = "Not implemented";
                    // }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Check should open these control windows for player
            if (subGameplayGeneralAttr.FactionTag == _playerFaction)
            {
                constructPanel.SetActive(true);
                if (isMainInfoSingleton )
                    ShouldOpenGarrisonInfoConjureQueueAndMiniConjure();
            }
            else
            {
                constructPanel.SetActive(false);
            }
        }

        private void ShouldOpenGarrisonInfoConjureQueueAndMiniConjure()
        {
            if (SubGameplayGarrisonInfoWindow.Instance.TrySwitchTarget(_targetEntity))
            {
                if (!SubGameplayGarrisonInfoWindow.Instance.IsOpened())
                    SubGameplayGarrisonInfoWindow.Instance.Show();
            }
            else SubGameplayGarrisonInfoWindow.Instance.Hide();

            if (ConjureQueueWindow.Instance.TrySwitchTarget(_targetEntity)
               )
            {
                if (!ConjureQueueWindow.Instance.IsOpened())
                    ConjureQueueWindow.Instance.Show();
            }
            else
                ConjureQueueWindow.Instance.Hide();

            if (MiniConjureWindow.Instance.TrySwitchTarget(_targetEntity)
               )
            {
                if (!MiniConjureWindow.Instance.IsOpened())
                    MiniConjureWindow.Instance.Show();
            }
            else MiniConjureWindow.Instance.Hide();
        }

        private void UpdateDynamicData()
        {
            
            _buildingAttr = Em.GetComponentData<BuildingAttr>(_targetEntity);
            // Visualize function panel and doingThings panel
            var underAttack = Em.HasComponent<OocTag>(_targetEntity) && Em.IsComponentEnabled<OocTag>(_targetEntity);
            var constructing = Em.HasComponent<ConstructingData>(_targetEntity);
            var generating = Em.HasComponent<GeneratingTag>(_targetEntity);
            var conjuring = Em.HasComponent<ConjuringTag>(_targetEntity);
            var currentState = BuildingUtils.GetBuildingState(underAttack, constructing, conjuring, generating);
            buildingStateIcon.sprite =
                BuildingWindowResourceManager.Instance.BuildingStateSprites[currentState];
            buildingStateText.text = currentState.ToString();
            if (_ifConstructing)
            {
                if (!Em.HasComponent<ConstructingData>(_targetEntity))
                {
                    ActiveNecessaryPanels(Em.GetComponentData<SubGameplayGeneralAttr>(_targetEntity));
                    _ifConstructing = false;
                    constructingPanel.SetActive(false);
                }
                else
                {
                    var time = Em.GetComponentData<ConstructingData>(_targetEntity).LastTime;
                    
                    constructTimeText.text = UIMathMethods.FormatTime((int)time);
                    return;   
                }
            }
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
                    var generateSpeed = 0f;
                    if (Em.HasComponent<ResourceMineGenerateAttr>(_targetEntity))
                    {
                        var generateAttribute = Em.GetComponentData<ResourceMineGenerateAttr>(_targetEntity);
                        generateSpeed = generateAttribute.CurGenerateSpeed;
                    }
                    else if(Em.HasComponent<PlantGenerateAttr>(_targetEntity))
                    {
                        var plantGenerateAttr = Em.GetComponentData<PlantGenerateAttr>(_targetEntity);
                        generateSpeed = plantGenerateAttr.GenerateSpeed;
                    }
                    generateSpeedText.text = $"{generateSpeed}";
                   
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
            
            if(isMainInfoSingleton)
                VisualizeCostSlots();
        }

        private void VisualizeCostSlots()
        {
            if (!isMainInfoSingleton)
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
                        costSlot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[cost.Type];
                        costSlot.label.text = cost.Type.ToString();
                        costSlot.value.text = $"x{cost.Amount}";
                    }
                    else
                    {
                        Slots[i].SetActive(false);
                    }
                }
            }
            else
            {
                // Main info singleton use cost slot to show how much cost to upgrade
                var list = CalculateUpgradeCostList();
                var isUpgradable = list.Count > 0;
                for (var i = 0; i < Slots.Count; i++)
                {
                    if (i < list.Count)
                    {
                        Slots[i].SetActive(true);
                        var cost = list[i];
                        var costSlot = SlotComponents[i];
                        
                        costSlot.icon.sprite = BasicUIResourceManager.Instance.ResourceSprites[cost.Type];
                        costSlot.label.text = cost.Type.ToString();
                        costSlot.value.text = $"x{cost.Amount}";
                        if (cost.Amount > _playerResources[cost.Type])
                        {
                            isUpgradable = false;
                            costSlot.label.color = notUpgradableColor;
                            costSlot.value.color = notUpgradableColor;
                        }
                        else
                        {
                            costSlot.label.color = Color.white;
                            costSlot.value.color = Color.white;
                        }
                        
                    }
                    else
                    {
                        Slots[i].SetActive(false);
                    }
                }
                
                upgradeButton.SetActive(isUpgradable); 
            }
           
        }

        private List<CostList> CalculateUpgradeCostList()
        {
            var list = new List<CostList>();
            if (Em.HasComponent<ExpData>(_targetEntity))
            {
                EcsGetExpStaticConfig?.Invoke(_targetEntity);
                var expDynamicData = Em.GetComponentData<ExpData>(_targetEntity);
                if (expDynamicData.curTier != ExpStaticConfig.MaxTier)
                {
                    var curCost = Em.GetBuffer<CostList>(_targetEntity);
                    var tarCost = Em.GetBuffer<CostList>(ExpStaticConfig.NextTierPrefab);
                    foreach (var costList in tarCost)
                    {
                        var e = costList;
                        foreach (var curCostList in curCost)
                        {
                            if (curCostList.Type == e.Type)
                            {
                                e.Amount = math.clamp(e.Amount - curCostList.Amount, 0, e.Amount);
                            }
                        }
                        list.Add(e);
                    }
                }
            }
            return list;
        }
    }
}
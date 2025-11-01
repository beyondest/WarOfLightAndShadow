using System;
using UnityEngine;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UI;

// ReSharper disable UseIndexFromEndExpression

namespace SparFlame.UI.MainGameplay
{
    [DisallowMultipleComponent]
    public class CityWindow3D : MonoBehaviour
    {
        [SerializeField] private GameObject iconPrefab; // 指向 GarrisonIconPrefab (UI Button+Image)

        // [SerializeField]
        // private int maxColumns = 6; // 最多列数
        [Header("Layout tuning")]
        [SerializeField] private Vector2 spacing = new(8, 8); // Grid spacing (pixels)
        // [SerializeField] private float minCellSize = 24f; // 最小 icon 尺寸（像素）
        // [SerializeField] private float maxCellSize = 80f; // 最大 icon 尺寸（像素）
        // [SerializeField] private float initCellSize = 50f;
        [SerializeField] private bool faceCameraOnlyY; // 只绕 Y 轴面对摄像机（可避免俯仰）
        [SerializeField] private bool followCamera = true;
        [Header("References")] [SerializeField]
        private TMP_Text garrisonCountText;

        [SerializeField] private GameObject panel;
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private TMP_Text leftFormingTime;
        [SerializeField] private GameObject formingPanel;
        [SerializeField] private Image formingArmyGroupImage;
        [SerializeField] private Image filledFormingBorder;


        public void Hide()
        {
            panel.SetActive(false);
        }

        public void Show()
        {
            panel.SetActive(true);
        }

        public void SetCity(Entity city)
        {
            _city = city;
            _cityGarrisonAttr = _em.GetComponentData<CityGarrisonAttr>(city);
            var generalAttr = _em.GetComponentData<MainGameplayGeneralAttr>(city);
            using var query = _em.CreateEntityQuery(typeof(PlayerFactionData));
            var playerFaction = query.GetSingleton<PlayerFactionData>();
            _isPlayerCity = FactionUtils.GetRelationship(generalAttr.faction, generalAttr.subFaction,
                playerFaction.faction, playerFaction.subFaction) is Relationship.Self or Relationship.Ally;
            _cityFaction = generalAttr.faction;
        }

        public void AddGarrison(Entity armyGroup)
        {
            if (_entityToSlot.ContainsKey(armyGroup)) return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);

            var slot = Instantiate(iconPrefab, contentRect);
            slot.name = "GarrisonIcon_" + armyGroupAttr.gameplayName;
            if (_isPlayerCity)
            {
                var btn = slot.GetComponent<CityGarrisonSlot3D>().button;
                btn?.onClick.AddListener(() => OnIconClicked(armyGroup));
            }

            var cityGarrisonSlot3D = slot.GetComponent<CityGarrisonSlot3D>();
            cityGarrisonSlot3D.SetTarget(armyGroup,_cityFaction);
            _entityToSlot[armyGroup] = slot.GetComponent<CityGarrisonSlot3D>();
            // UpdateLayout();
        }

        public void RemoveGarrison(Entity armyGroup)
        {
            if (!_entityToSlot.TryGetValue(armyGroup, out var g)) return;
            Destroy(g.gameObject);
            _entityToSlot.Remove(armyGroup);

            // UpdateLayout();
        }

        public void ResetWhenCityFactionChanged()
        {
            foreach (var pair in _entityToSlot)
            {
                Destroy(pair.Value.gameObject);
            }
            _entityToSlot.Clear();
            SetCity(_city);
        }

        // run-time

        private readonly Dictionary<Entity, CityGarrisonSlot3D> _entityToSlot = new();
        private EntityManager _em;
        private Entity _city;
        private CityGarrisonAttr _cityGarrisonAttr;
        private FactionTag _cityFaction;
        private Camera _targetCamera;
        private EntityQuery _worldTimeQuery;
        private EntityQuery _gameStatusDataQuery;
        private bool _isPlayerCity;
        private Canvas _canvas;
        private EnemyAIMainGameplayDebug _debug;

        private void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            DontDestroyOnLoad(gameObject);
            _canvas = GetComponent<Canvas>();
            
        }

        private void Start()
        {
            grid.spacing = spacing;
            _worldTimeQuery = _em.CreateEntityQuery(typeof(WorldTimeData));
            _gameStatusDataQuery = _em.CreateEntityQuery(typeof(GameStatusData));

            using var debugTag = _em.CreateEntityQuery(typeof(DebugTag));
            using var enemyAIDebug = _em.CreateEntityQuery(typeof(EnemyAIMainGameplayDebug));
            if (!debugTag.IsEmpty && !enemyAIDebug.IsEmpty)
                _debug = enemyAIDebug.GetSingleton<EnemyAIMainGameplayDebug>();
        }

        private void Update()
        {
            var gameStatusData = _gameStatusDataQuery.GetSingleton<GameStatusData>();
            if(gameStatusData.Value != GameStatus.MainGaming )return;
            if (!CityDetailWindow.Instance.IsOpened()
                || CityDetailWindow.Instance.GetTarget() != _city
                || !_em.HasBuffer<ArmyGroupConjureStack>(_city))
            {
                Hide();
                return;
            }

            var notEmptyArmyGroupsCount = 0;
            foreach (var pair in _entityToSlot)
            {
                if (_em.HasBuffer<ArmyGroupUnit>(pair.Key))
                {
                    var units = _em.GetBuffer<ArmyGroupUnit>(pair.Key);
                    if (units.Length > 0)
                    {
                        notEmptyArmyGroupsCount++;
                        pair.Value.panel.SetActive(true);
                        var statData = _em.GetComponentData<ArmyGroupStatData>(pair.Key);
                        pair.Value.UpdateHp(
                            statData.totalMaxHp == 0 ? 0 : statData.totalCurrentHp / statData.totalMaxHp);
                    }
                    else
                    {
                        pair.Value.panel.SetActive(false);
                    }
                }
            }

            garrisonCountText.text = $"{notEmptyArmyGroupsCount}/{_cityGarrisonAttr.maxGarrisonCount}";

            if (_isPlayerCity)
            {
                formingPanel.SetActive(false);
                return;
            }
            var stack = _em.GetBuffer<ArmyGroupConjureStack>(_city);
            formingPanel.SetActive(!stack.IsEmpty);
            if (!stack.IsEmpty)
            {
                var armyGroup = stack[stack.Length - 1];
                var worldTime = _worldTimeQuery.GetSingleton<WorldTimeData>();
                var aiData = _em.GetComponentData<CityAIData>(_city);
                var needHours = _debug.enabled
                    ? armyGroup.NeedHours * _debug.armyGroupConjureTimeScale
                    : armyGroup.NeedHours;

                var leftHours = math.max(0f,
                    needHours - (worldTime.totalHours - aiData.StartConjuringTotalHours));
                var iconType = DatabaseManager.ArmyGroupDatabaseSo.GetItemById(armyGroup.PrefabId).iconType;
                leftFormingTime.text = $"{UIMathMethods.FormatTimeFromHours(leftHours)}";
                formingArmyGroupImage.sprite =
                    ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[iconType];
                filledFormingBorder.fillAmount = needHours != 0 ? 1f - leftHours / armyGroup.NeedHours : 0;
            }
        }

        
        
        private void LateUpdate()
        {
            _targetCamera = Camera.main;
            if (!followCamera || !panel || !_targetCamera ) return;
            _canvas.worldCamera = _targetCamera;
            // 使 Canvas 面向摄像机
            if (faceCameraOnlyY)
            {
                var dir = _targetCamera.transform.position - panel.transform.position;
                dir.y = 0; // 保持竖直朝向
                if (dir.sqrMagnitude > 0.0001f)
                    panel.transform.rotation = Quaternion.LookRotation(-dir.normalized);
            }
            else
            {
                var dir = _targetCamera.transform.position - panel.transform.position;
                // 完全面向摄像机（包括俯仰）
                panel.transform.rotation =
                    Quaternion.LookRotation(-dir);
            }
        }


        private void OnDestroy()
        {
            try
            {
                if(_worldTimeQuery != default)
                    _worldTimeQuery.Dispose();
                if(_gameStatusDataQuery != default)
                    _gameStatusDataQuery.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnIconClicked(Entity armyGroup)
        {
            var request = _em.CreateEntity();
            _em.AddComponent<ArmyGroupGarrisonRequest>(request);
            _em.SetComponentData(request, new ArmyGroupGarrisonRequest
            {
                City = _city,
                ArmyGroup = armyGroup,
                IfGarrisonIn = false,
            });
            _em.AddComponent<MainGameplayEntityTag>(request);
        }

        /*private void UpdateLayout()
        {
            if (!contentRect || !grid) return;
            var count = contentRect.childCount;
            if (count == 0)
            {
                // 如果没有驻军，保留 Grid 原始 cellSize 或隐藏 Canvas
                return;
            }

            // 1) 计算列数（不超过 maxColumns）
            var columns = Mathf.Max(1, Mathf.Min(maxColumns, count));
            var rows = Mathf.CeilToInt(count / (float)columns);

            // 2) 用 Content Rect 的宽高计算每个 cell 可以占多少像素
            var width = contentRect.rect.width;
            var height = contentRect.rect.height;

            var totalSpacingX = grid.spacing.x * (columns - 1) + grid.padding.left + grid.padding.right;
            var totalSpacingY = grid.spacing.y * (rows - 1) + grid.padding.top + grid.padding.bottom;

            var cellW = (width - totalSpacingX) / columns;
            var cellH = (height - totalSpacingY) / rows;

            var cellSize = Mathf.Floor(Mathf.Min(cellW, cellH));
            cellSize = Mathf.Clamp(cellSize, minCellSize, maxCellSize);

            var ratio = cellSize / initCellSize;
            foreach (var slot in _entityToSlot.Values)
            {
                slot.UpdateSize(ratio);
            }

            // 3) 应用到 GridLayoutGroup
            grid.cellSize = new Vector2(cellSize, cellSize);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;

            // 4) 强制刷新布局
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }*/
    }
}
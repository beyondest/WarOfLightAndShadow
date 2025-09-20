using UnityEngine;
using System.Collections.Generic;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
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

        [Header("Layout tuning")] [SerializeField]
        private int maxColumns = 6; // 最多列数

        [SerializeField] private Vector2 spacing = new(8, 8); // Grid spacing (pixels)
        [SerializeField] private float minCellSize = 24f; // 最小 icon 尺寸（像素）
        [SerializeField] private float maxCellSize = 80f; // 最大 icon 尺寸（像素）
        [SerializeField] private float initCellSize = 50f;
        [SerializeField] private bool faceCameraOnlyY; // 只绕 Y 轴面对摄像机（可避免俯仰）

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
            _cityAttr = _em.GetComponentData<CityAttr>(city);
        }

        // 添加一个驻军 UI（调用方负责传入唯一 id、Sprite，以及可选的 3D billboard GameObject 用来隐藏）
        public void AddGarrison(Entity armyGroup)
        {
            if (_entityToSlot.ContainsKey(armyGroup)) return;

            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var armyGroupAttr = em.GetComponentData<ArmyGroupAttr>(armyGroup);

            var slot = Instantiate(iconPrefab, contentRect);
            slot.name = "GarrisonIcon_" + armyGroupAttr.gameplayName;
            var btn = slot.GetComponent<CityGarrisonSlot3D>().button;
            if (btn)
            {
                btn.onClick.AddListener(() => OnIconClicked(armyGroup));
            }

            var cityGarrisonSlot3D = slot.GetComponent<CityGarrisonSlot3D>();
            cityGarrisonSlot3D.SetTarget(armyGroup);


            _entityToSlot[armyGroup] = slot.GetComponent<CityGarrisonSlot3D>();
            UpdateLayout();
        }

        public void RemoveGarrison(Entity armyGroup)
        {
            if (!_entityToSlot.TryGetValue(armyGroup, out var g)) return;
            Destroy(g.gameObject);
            _entityToSlot.Remove(armyGroup);

            UpdateLayout();
        }


        // run-time

        private readonly Dictionary<Entity, CityGarrisonSlot3D> _entityToSlot = new();
        private EntityManager _em;
        private Entity _city;
        private CityAttr _cityAttr;
        private Camera _targetCamera;
        private EntityQuery _worldTimeQuery;

        private void Awake()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Start()
        {
            grid.spacing = spacing;
            _worldTimeQuery = _em.CreateEntityQuery(typeof(WorldTimeData));
        }

        private void Update()
        {
            if (!CityDetailWindow.Instance.IsOpened()
                || CityDetailWindow.Instance.GetTarget() != _city)
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

            garrisonCountText.text = $"{notEmptyArmyGroupsCount}/{_cityAttr.maxGarrisonCount}";


            var stack = _em.GetBuffer<ArmyGroupConjureStack>(_city);
            formingPanel.SetActive(!stack.IsEmpty);
            if (!stack.IsEmpty)
            {
                var armyGroup = stack[stack.Length - 1];
                var worldTime = _worldTimeQuery.GetSingleton<WorldTimeData>();
                var aiData = _em.GetComponentData<CityAIData>(_city);

                var leftHours = math.max(0f,
                    armyGroup.NeedHours - (worldTime.totalHours - aiData.StartConjuringTotalHours));
                var armyGroupAttr = _em.GetComponentData<ArmyGroupAttr>(armyGroup.ArmyGroupPrefab);
                leftFormingTime.text = $"{UIMathMethods.FormatTimeFromHours(leftHours)}";
                formingArmyGroupImage.sprite =
                    ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
                filledFormingBorder.fillAmount = armyGroup.NeedHours != 0 ? 1f - leftHours / armyGroup.NeedHours : 0;
            }
        }

        private void LateUpdate()
        {
            _targetCamera = Camera.main;
            if (!panel || !_targetCamera) return;

            // 使 Canvas 面向摄像机
            if (faceCameraOnlyY)
            {
                var dir = _targetCamera.transform.position - panel.transform.position;
                dir.y = 0; // 保持竖直朝向
                if (dir.sqrMagnitude > 0.0001f)
                    panel.transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
            else
            {
                // 完全面向摄像机（包括俯仰）
                panel.transform.rotation =
                    Quaternion.LookRotation(_targetCamera.transform.position - panel.transform.position);
            }
        }


        // Icon 点击事件（在这里触发选择/出城等逻辑）
        private void OnIconClicked(Entity armyGroup)
        {
            Debug.Log($"Garrison icon clicked: {armyGroup} ");
        }

        // 核心：根据当前 icon 数量计算列数与 cellSize，然后强制刷新布局
        private void UpdateLayout()
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
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using SparFlame.Core.Interfaces;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SparFlame.UI.General
{
    public class MultiShowSlot : MonoBehaviour
    {
        public int Index { get; set; }
        [CanBeNull] public Button button;

        
    }
    public static class MultiSlotWindowUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="slots">Must have component variant from MultiSlot
        /// and must have RectTransform</param>
        /// <param name="slotComponents"></param>
        /// <param name="panel"></param>
        /// <param name="slotPrefab"></param>
        /// <param name="config"></param>
        /// <param name="onClickSlot">This function will call when click on slot button 
        /// but notice that if you use page techniques to show info counts bigger than max slot count per page,
        /// you have to add bias via current page by yourself</param>
        private static void InstantiateMultiShowSlotsByIndex<TMultiShowSlot>(List<GameObject> slots,
            List<TMultiShowSlot> slotComponents,
            GameObject panel, GameObject slotPrefab, in MultiShowSlotConfig config,
            [CanBeNull] Action<int> onClickSlot = null) where TMultiShowSlot : MultiShowSlot
        {
            var panelRect = panel.GetComponent<RectTransform>();
            float cellHeight;
            float cellWidth;
            if (config.autoCellSize)
            {
                cellWidth = (panelRect.rect.width - (config.cols - 1) * config.columnSpacing) / config.cols;
                cellHeight = (panelRect.rect.height - (config.rows - 1) * config.rowSpacing) / config.rows;
            }
            else
            {
                var rect = slotPrefab.GetComponent<RectTransform>();
                cellHeight = rect.rect.height;
                cellWidth = rect.rect.width;
            }

            if (config.ifSquare)
            {
                for (var r = 0; r < config.rows; r++)
                {
                    for (var c = 0; c < config.cols; c++)
                    {
                        var slot = Object.Instantiate(slotPrefab, panel.transform);
                        var slotRect = slot.GetComponent<RectTransform>();
                        var posX = config.startPos.x + c * (cellWidth + config.columnSpacing);
                        var posY = config.startPos.y - r * (cellHeight + config.rowSpacing);
                        slotRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                        slotRect.anchoredPosition = new Vector2(posX, posY);
                        slot.SetActive(config.shouldSlotShow);
                        var slotComponent = slot.GetComponent<TMultiShowSlot>();
                        slotComponent.Index = r * config.cols + c;
                        if (onClickSlot != null && slotComponent.button)
                        {
                            // TODO : Extend original button class to support right click event and long click event
                            slotComponent.button.onClick.AddListener(() => { onClickSlot(slotComponent.Index); });
                        }
                        slots.Add(slot);
                        slotComponents.Add(slotComponent);
                    }
                }
            }
            else
            {
                Assert.IsFalse(config.autoCellSize); // Circle must not be auto cell size
                var points =
                    MathUtils.GenerateCirclePoints(config.center, config.radius, config.circleElemCount);
                for (var i = 0; i < config.circleElemCount; i++)
                {
                    var slot = Object.Instantiate(slotPrefab, panel.transform);
                    var slotRect = slot.GetComponent<RectTransform>();
                    slotRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    slotRect.anchoredPosition = points[(i + config.elemBias) % config.circleElemCount];
                    slot.SetActive(config.shouldSlotShow);

                    var slotComponent = slot.GetComponent<TMultiShowSlot>();
                    slotComponent.Index = i;
                    if (onClickSlot != null && slotComponent.button != null)
                        slotComponent.button.onClick.AddListener((() => { onClickSlot(slotComponent.Index); }));
                    slots.Add(slot);
                    slotComponents.Add(slotComponent);
                }
            }
        }

        public interface IUIWindow
        {
            public void Show(Vector2? pos = null);
            public void Hide();
            public bool IsOpened();
        }

        public interface ISingleTargetWindow : IUIWindow
        {
            public bool TrySwitchTarget(Entity target);
            public bool HasTarget();
            public void ClearCloseUpTarget();
        }

        public class MultiSlotsWindow<TMultiShowSlot> : MonoBehaviour,IResourceManager,
            IUIWindow where TMultiShowSlot : MultiShowSlot
        {
            [SerializeField] protected GameObject panel;
            [SerializeField] private AssetReferenceGameObject slotPrefab;
            [SerializeField, ShowIf(nameof(multiSlotEnabled))] protected MultiShowSlotConfig config;
            [SerializeField]
            protected bool multiSlotEnabled = true;

            protected AsyncOperationHandle<GameObject> SlotPrefabHandle;

            private GameObject _slotPrefab;
            
            protected readonly List<GameObject> Slots = new();
            protected readonly List<TMultiShowSlot> SlotComponents = new();
            
 
            public virtual void OnClickSlot(int slotIndex)
            {
            }

            public virtual void Show(Vector2? pos = null)
            {
                panel.SetActive(true);
            }

            public virtual void Hide()
            {
                panel.SetActive(false);
            }

            public virtual bool IsOpened()
            {
                return panel.activeSelf;
            }
            
            public virtual bool IsResourceLoaded()
            {
                if (!multiSlotEnabled) return true;
                if (SlotPrefabHandle.IsValid() && SlotPrefabHandle.IsDone)
                {
                    if(SlotComponents.Count ==0)return true;
                    return SlotComponents.All(slot =>
                    {
                        if (slot is IResourceManager resourceManager)
                        {
                            return resourceManager.IsInitialized;
                        }
                        return true;
                    });
                }
                else
                {
                    return false;
                }
            }
            public bool IsInitialized => IsResourceLoaded();
            public float InitProgress => IsResourceLoaded() ? 1f : 0f;
            public virtual void LoadResources()
            {
                if (!multiSlotEnabled) return;
                SlotPrefabHandle = ResourceLoadingUtils.LoadAssetRefAsync<GameObject>(slotPrefab,
                    go =>
                    {
                        _slotPrefab = go;
                        InstantiateMultiShowSlotsByIndex(Slots, SlotComponents, panel,
                            _slotPrefab, in config, OnClickSlot);
                        foreach (var slot in SlotComponents)
                        {
                            if (slot is IResourceManager resourceManager)
                            {
                                resourceManager.LoadResources();
                            }
                        }
                    });
            }

            public virtual void UnloadResources()
            {
                if(!multiSlotEnabled) return;
                Addressables.Release(SlotPrefabHandle);
                for (var i = 0; i < Slots.Count; i++)
                {
                    var slot = Slots[i];
                    var slotComp = SlotComponents[i];
                    if (slotComp is IResourceManager resourceManager)
                    {
                        resourceManager.UnloadResources();
                    }
                    Destroy(slot);
                }
                Slots.Clear();
                SlotComponents.Clear();
            }

            protected virtual void Start()
            {
                GeneralResourceManager.Instance.Register(this);
            }
        }


        [Serializable]
        public struct MultiShowSlotConfig
        {
            [Header("General Config")] public bool ifSquare;
            public bool shouldSlotShow;
            [Header("Square Config")] [Tooltip("If disable auto cell size, will use prefab width and height")]
            public bool autoCellSize;

            public int rows;
            public int cols;

            [Tooltip("This is the anchor position of prefab")]
            public Vector2 startPos;

            public float columnSpacing;
            public float rowSpacing;

            [Header("Circle Config")] public float radius;
            public int circleElemCount;

            [Tooltip("This bias is used when you want to change the start position of first slot")]
            public int elemBias;

            public Vector2 center;
        }
    }
}
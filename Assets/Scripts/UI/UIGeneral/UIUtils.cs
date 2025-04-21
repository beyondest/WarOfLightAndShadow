using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using SparFlame.Utils;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SparFlame.UI.General
{
    public static class UIUtils
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
                        slot.SetActive(false);
                        var slotComponent = slot.GetComponent<TMultiShowSlot>();
                        slotComponent.Index = r * config.cols + c;
                        if (onClickSlot != null && slotComponent.button != null)
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
                    CustomMathMethods.GenerateCirclePoints(config.center, config.radius, config.circleElemCount);
                for (var i = 0; i < config.circleElemCount; i++)
                {
                    var slot = Object.Instantiate(slotPrefab, panel.transform);
                    var slotRect = slot.GetComponent<RectTransform>();
                    slotRect.sizeDelta = new Vector2(cellWidth, cellHeight);
                    slotRect.anchoredPosition = points[(i + config.elemBias) % config.circleElemCount];
                    slot.SetActive(false);

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
        }


        public class
            MultiSlotsWindow<TMultiShowSlot> : MonoBehaviour,
            IUIWindow where TMultiShowSlot : MultiShowSlot
        {
            [SerializeField] protected GameObject panel;
            [SerializeField] private AssetReferenceGameObject slotPrefab;
            [SerializeField] protected MultiShowSlotConfig config;

            public bool shouldReleasePrefabsWhenDisabled;

            protected AsyncOperationHandle<GameObject> SlotPrefabHandle;

            private bool _initialized;
            private GameObject _slotPrefab;
            protected readonly List<GameObject> Slots = new();
            protected readonly List<TMultiShowSlot> SlotComponents = new();

            /// <summary>
            /// TODO : Add not use multi slots support, for building detail main window
            /// </summary>
            public void SetInitialized()
            {
                _initialized = true;
            }
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

            

            protected virtual void OnEnable()
            {
                if (_initialized) return;
                _initialized = true;
                SlotPrefabHandle = CR.LoadAssetRefAsync<GameObject>(slotPrefab,
                    go =>
                    {
                        _slotPrefab = go;
                        InstantiateMultiShowSlotsByIndex(Slots, SlotComponents, panel,
                            _slotPrefab, in config, OnClickSlot);
                    });
            }

            protected virtual void OnDisable()
            {
                if (!shouldReleasePrefabsWhenDisabled) return;
                _initialized = false;
                Addressables.Release(SlotPrefabHandle);
                foreach (var slot in Slots)
                {
                    Destroy(slot);
                }
                Slots.Clear();
                SlotComponents.Clear();
            }

            public virtual bool IsResourceLoaded()
            {
                return _initialized && SlotPrefabHandle.IsValid() && SlotPrefabHandle.IsDone;
            }
        }


        [Serializable]
        public struct MultiShowSlotConfig
        {
            [Header("Choose Square or Circle")] public bool ifSquare;

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
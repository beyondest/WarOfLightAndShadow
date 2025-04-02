using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
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
        public static void InstantiateMultiShowSlotsByIndex<TMultiShowSlot>(List<GameObject> slots,
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
            MultiSlotsWindow<TMultiShowSlot, TCustomMonobehaviour> : MonoBehaviourSingleton<TCustomMonobehaviour>,
            IUIWindow where TMultiShowSlot : MultiShowSlot where TCustomMonobehaviour : MonoBehaviour
        {
            [SerializeField] protected GameObject panel;
            [SerializeField] private AssetReferenceGameObject slotPrefab;
            [SerializeField] protected MultiShowSlotConfig config;


            protected AsyncOperationHandle<GameObject> SlotPrefabHandle;

            protected GameObject SlotPrefab;
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


            protected virtual void OnEnable()
            {
                SlotPrefabHandle = CR.LoadAssetRefAsync<GameObject>(slotPrefab,
                    go =>
                    {
                        SlotPrefab = go;
                        InstantiateMultiShowSlotsByIndex(Slots, SlotComponents, panel,
                            SlotPrefab, in config, OnClickSlot);
                    });
                panel.SetActive(false);
            }

            protected virtual void OnDisable()
            {
                Addressables.Release(SlotPrefabHandle);
                foreach (var slot in Slots)
                {
                    Destroy(slot);
                }

                Slots.Clear();
                SlotComponents.Clear();
            }
        }

     


        [Serializable]
        public struct MultiShowSlotConfig
        {
            [Tooltip("If disable auto cell size, will use prefab width and height")]
            public bool autoCellSize;

            public int rows;
            public int cols;

            [Tooltip("This is the anchor position of prefab")]
            public Vector2 startPos;

            public float columnSpacing;
            public float rowSpacing;
        }
    }
}
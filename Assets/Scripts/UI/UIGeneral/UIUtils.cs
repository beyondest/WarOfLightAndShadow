using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SparFlame.UI.General
{
    public static class UIUtils  
    {
        public static void InitMultiShowSlots(ref List<GameObject> slots,
            GameObject panel,GameObject slotPrefab,in MultiShowSlotConfig config,
            [CanBeNull] Action<int> onClickSlot = null)
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
                var rect  = slotPrefab.GetComponent<RectTransform>();
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
                    var slotMono = slot.GetComponent<MultiShowSlot>();
                    slotMono.index = r * config.cols + c;
                    if(onClickSlot != null && slotMono.button != null)
                        slotMono.button.onClick.AddListener((() => { onClickSlot(slotMono.index); }));
                    slots.Add(slot);
                }
            }
            panel.SetActive(false);
        }
        
        public static Dictionary<T, Sprite> LoadTypeSprites<T>(string path, string prefix = null) where T : Enum
        {
            var dict = new Dictionary<T, Sprite>();

            foreach (T type in Enum.GetValues(typeof(T)))
            {
                string fullName = type.ToString();
                if (!string.IsNullOrEmpty(prefix))
                    fullName = prefix + fullName;

                string fullPath = $"{path}/{fullName}";  
                Sprite sprite = Resources.Load<Sprite>(fullPath);

                if (sprite != null)
                {
                    dict[type] = sprite;
                }
                else
                {
                    Debug.LogError($"Sprite not found: {fullPath}");
                }
            }
            return dict;
        }
    }
  


    public abstract class UIWindow : MonoBehaviour
    {
        public abstract void Show(Vector2? pos=null);
        public abstract void Hide();
        public abstract bool IsOpened();
    }

    public abstract class SingleTargetWindow : UIWindow
    {
        public abstract bool TrySwitchTarget(Entity target);
    }
    
    public class MultiShowSlot : MonoBehaviour
    {
        public int index { get; set; }
        [CanBeNull] public Button button;
    }
    
    
    

    [Serializable]
    public struct MultiShowSlotConfig
    {
        [Tooltip("If disable auto cell size, will use prefab width and height")]
        public bool autoCellSize;
        public int rows ;
        public int cols ;
        [Tooltip("This is the anchor position of prefab")]
        public Vector2 startPos;
        public float columnSpacing;
        public float rowSpacing;
    }
}
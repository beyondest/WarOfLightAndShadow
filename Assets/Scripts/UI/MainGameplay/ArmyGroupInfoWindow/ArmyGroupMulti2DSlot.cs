using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupMulti2DSlot : MultiShowSlot
    {
        public Image filledHp;
        public TMP_Text levelAndCount;
        
        // public GameObject nonZeroPanel;  
        // public GameObject nonZero1Panel;
        // public GameObject nonZero2Panel;
        // public GameObject nonZero3Panel;


        // public TMP_Text slot1;
        // public Image slot1Image;
        // public TMP_Text slot2;
        // public Image slot2Image;
        // public TMP_Text slot3;
        // public Image slot3Image;
        //
        // public TMP_Text slot4;
        // public TMP_Text slot5;
        // public TMP_Text slot6;

        public void SetTarget(in ArmyGroupMulti2DRealTimeInfo info,
            FactionTag currentSelectFaction)
        {
            button!.image.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[info.IconType];
            button.image.color = currentSelectFaction == FactionTag.Light ? Color.white : Color.black;
            filledHp.fillAmount = info.TotalHpRatio;
            levelAndCount.text = $"Lv.{info.AvgLevel} / {info.UnitCounts}";
         

            // var nonZeroCount = info.UnitCountPerTier.Count(count => count != 0);
            // switch (nonZeroCount)
            // {
            //     case 1:
            //         nonZero1Panel.SetActive(true);
            //         nonZero2Panel.SetActive(false);
            //         nonZero3Panel.SetActive(false);
            //         var index = info.UnitCountPerTier.FindIndex(count => count != 0);
            //         var tier = (Tier)(index + 3);
            //         var c = info.UnitCountPerTier[index];
            //         slot1.text = c.ToString();
            //         slot1Image.sprite = BasicUIResourceManager.Instance.TierSprites[tier];
            //         break;
            //     case 2:
            //         nonZero2Panel.SetActive(true);
            //         nonZero3Panel.SetActive(false);
            //         nonZero1Panel.SetActive(false);
            //         var index1 = info.UnitCountPerTier.FindIndex(count => count != 0);
            //         var tier1 = (Tier)(index1 + 3);
            //         var c1 = info.UnitCountPerTier[index1];
            //         var index2 = info.UnitCountPerTier.FindLast(count => count != 0);
            //         var tier2 = (Tier)(index2 + 3);
            //         var c2 = info.UnitCountPerTier[index2];
            //         slot2.text = c1.ToString();
            //         slot2Image.sprite = BasicUIResourceManager.Instance.TierSprites[tier1];
            //         slot3.text = c2.ToString();
            //         slot3Image.sprite = BasicUIResourceManager.Instance.TierSprites[tier2];
            //         break;
            //     case 3:
            //         nonZero3Panel.SetActive(true);
            //         nonZero1Panel.SetActive(false);
            //         nonZero2Panel.SetActive(false);
            //         slot4.text = info.UnitCountPerTier[0].ToString();
            //         slot5.text = info.UnitCountPerTier[1].ToString();
            //         slot6.text = info.UnitCountPerTier[2].ToString();
            //         break;
            // }
        }
    }
}
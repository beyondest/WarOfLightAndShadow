using System;
using SparFlame.Components.General;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.BasicControl;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows
{
    public class CrystalHpWindow : MonoBehaviour
    {
        [Header("Crystal Hp")] [SerializeField]
        private Image crystalHpFilled;
        [SerializeField] private Image crystalHpBlank;
       
        
        private EntityQuery _crystalQuery;
        private EntityQuery _subGameStatusQuery;

        private void Start()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _crystalQuery = em.CreateEntityQuery(typeof(CrystalDef), typeof(StatData),
                typeof(SubGameplayGeneralAttr));
            _subGameStatusQuery = em.CreateEntityQuery(typeof(SubGameStatusData));
            crystalHpFilled.enabled = false;
            crystalHpBlank.enabled = false;
        
        }

        private void Update()
        {
            var subGameStatusData = _subGameStatusQuery.GetSingleton<SubGameStatusData>();
            if (!GameStatusUtils.IsInBattle(subGameStatusData)
                || _crystalQuery.IsEmpty)
            {
                crystalHpFilled.enabled = false;
                crystalHpBlank.enabled = false;
                return;
            }
            var statData = _crystalQuery.GetSingleton<StatData>();
            var generalAttr = _crystalQuery.GetSingleton<SubGameplayGeneralAttr>();
            
            crystalHpFilled.enabled = true;
            crystalHpBlank.enabled = true;
            crystalHpFilled.fillAmount = statData.curValue / statData.maxValue;
            crystalHpFilled.sprite =
                BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[generalAttr.Faction];
            crystalHpBlank.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[generalAttr.Faction];


        }

        private void OnDestroy()
        {
            try
            {
                if(_crystalQuery != default)
                    _crystalQuery.Dispose();
                if(_subGameStatusQuery != default)
                    _subGameStatusQuery.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
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
        private Image playerCrystalFilledHp;

        [SerializeField] private Image playerCrystalBlankHp;
        [SerializeField] private Image enemyCrystalFilledHp;
        [SerializeField] private Image enemyCrystalBlankHp;
        
        private FactionTag _playerFaction;
        
        private EntityQuery _playerCrystalInfo;
        private EntityQuery _enemyCrystalInfo;

        private void Start()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _playerCrystalInfo = em.CreateEntityQuery(typeof(PlayerCrystalInfo));
            _enemyCrystalInfo = em.CreateEntityQuery(typeof(EnemyCrystalInfo));
            GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
            GeneralResourceManager.Instance.OnAllResourceLoaded += UpdateStaticInfo;
        }

        private void Update()
        {
            var enemyInfo = _enemyCrystalInfo.GetSingleton<EnemyCrystalInfo>();
            var playerInfo = _playerCrystalInfo.GetSingleton<PlayerCrystalInfo>();
            // Update hp info
            enemyCrystalFilledHp.enabled = enemyInfo.InSightValidCount != 0;
            enemyCrystalBlankHp.enabled = enemyInfo.InSightValidCount != 0;
            // Update crystal hp info
            if (playerInfo.MaxTotalHp != 0f)
                playerCrystalFilledHp.fillAmount = playerInfo.CurTotalHp / playerInfo.MaxTotalHp;
            if (enemyInfo.MaxTotalHp != 0f)
                enemyCrystalFilledHp.fillAmount = enemyInfo.CurTotalHp / enemyInfo.MaxTotalHp;

        }
        
        private void UpdateStaticInfo()
        {
            playerCrystalFilledHp.sprite =
                BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[_playerFaction];
            playerCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[_playerFaction];

            enemyCrystalFilledHp.sprite =
                BasicUIResourceManager.Instance.FactionCrystalHpFilledSprites[~_playerFaction];
            enemyCrystalBlankHp.sprite = BasicUIResourceManager.Instance.FactionCrystalHpBlankSprites[~_playerFaction];
        }
    }
}
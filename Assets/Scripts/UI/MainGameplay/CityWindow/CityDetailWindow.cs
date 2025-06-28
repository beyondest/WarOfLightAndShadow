using System;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class CityDetailWindow :MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {
        #region Config

        [SerializeField] private TMP_Text cityNameText;
        [SerializeField] private TMP_Text cityDescriptionText;
        [SerializeField] private Image generalFactionImage;
        [SerializeField] private Image subFactionImage;
        
        [Header("Panels")]
        [SerializeField] private GameObject panel;
        [SerializeField] private GameObject controlPanel;

        #endregion


        #region Interface

        

        public static CityDetailWindow Instance;
       

        public void Show(Vector2? pos = null)
        {
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
            _targetEntity = Entity.Null;
        }

        public bool IsOpened()
        {
           return panel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if(!_em.HasComponent<CityAttr>(target))return false;
            _targetEntity = target;
            UpdateStaticData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity!= Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        #endregion

        #region ButtonMethods

        public void OnClickEnterCity()
        {
            var cityAttr = _em.GetComponentData<CityAttr>(_targetEntity);
            GameController.Instance.EnterPlayerCity(_targetEntity);
        }
        

        #endregion


       
        // Internal Data
        private Entity _targetEntity;
        private EntityManager _em;
        private FactionTag _playerFaction;
        
        #region EventFunctions

        

        
        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            GameController.Instance.OnPlayerChooseFaction += factionTag => _playerFaction = factionTag;
            Hide();
        }

   

        #endregion


        private void UpdateStaticData()
        {
            var generalAttr = _em.GetComponentData<MainGameplayGeneralAttr>(_targetEntity);
            var cityAttr = _em.GetComponentData<CityAttr>(_targetEntity);
            if (generalAttr.faction == _playerFaction)
            {
                controlPanel.SetActive(true);
            }

            var idStart = DatabaseManager.CityDatabaseSo.idStart;
            var item = DatabaseManager.CityDatabaseSo.items[cityAttr.globalId - idStart];
            cityNameText.text = item.gameplayName;
            cityDescriptionText.text = item.description;
            generalFactionImage.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[generalAttr.faction];
            subFactionImage.sprite = BasicUIResourceManager.Instance.SubFactionIconSprites[generalAttr.subFaction];
            if(CityGarrisonWindow.Instance.TrySwitchTarget(_targetEntity))
                CityGarrisonWindow.Instance.Show();
            else
                CityGarrisonWindow.Instance.Hide();
        }

        
        
    }
    
    
}
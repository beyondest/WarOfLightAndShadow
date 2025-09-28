using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.MainGameplay.ArmyGroup;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class CityDetailWindow : MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {
        #region Config

        [SerializeField] private TMP_Text cityNameText;
        [SerializeField] private TMP_Text cityDescriptionText;
        [SerializeField] private Image generalFactionImage;
        [SerializeField] private Image subFactionImage;

        [Header("Panels")] [SerializeField] private GameObject panel;
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
            if (_em.HasComponent<CityWindow3DComponent>(_targetEntity))
            {
                var cityWindow = _em.GetComponentData<CityWindow3DComponent>(_targetEntity);
                cityWindow.CityWindow3D.Hide();
            }
            _targetEntity = Entity.Null;
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<CityAttr>(target)) return false;
            _targetEntity = target;
            UpdateStaticData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }
        public Entity GetTarget() => _targetEntity;

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        #endregion

        #region ButtonMethods

        public void OnClickEnterCity()
        {
            StartCoroutine(GameController.Instance.EnterPlayerCity(_targetEntity));
        }

        #endregion


        // Internal Data
        private Entity _targetEntity;
        private EntityManager _em;

        #region EventFunctions

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Hide();
        }

        #endregion


        private void UpdateStaticData()
        {
            var generalAttr = _em.GetComponentData<MainGameplayGeneralAttr>(_targetEntity);
            var prefabId = _em.GetComponentData<PrefabId>(_targetEntity);
            using var query = _em.CreateEntityQuery(typeof(PlayerFactionData));
            var playerFactionData =query.GetSingleton<PlayerFactionData>();
            var relationShip =
                FactionUtils.GetRelationship(playerFactionData.faction,
                    playerFactionData.subFaction, generalAttr.faction, generalAttr.subFaction);
            controlPanel.SetActive(relationShip == Relationship.Self
                                   || (relationShip == Relationship.Ally &&
                                       !_em.HasComponent<SupportFightTag>(_targetEntity)));

            var item = DatabaseManager.CityDatabaseSo.GetItemById(prefabId.value);
            cityNameText.text = item.gameplayName;
            cityDescriptionText.text = item.description;
            generalFactionImage.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[generalAttr.faction];
            subFactionImage.sprite = BasicUIResourceManager.Instance.SubFactionIconSprites[generalAttr.subFaction];
            
            // if (CityGarrisonWindow.Instance.TrySwitchTarget(_targetEntity))
            //     CityGarrisonWindow.Instance.Show();
            // else
            //     CityGarrisonWindow.Instance.Hide();

            if (_em.HasComponent<CityWindow3DComponent>(_targetEntity))
            {
                var cityWindow = _em.GetComponentData<CityWindow3DComponent>(_targetEntity);
                cityWindow.CityWindow3D.Show();
            }
        }
    }
}
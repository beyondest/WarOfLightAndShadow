using SparFlame.Database;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    public class ResourceDetailWindow : MonoBehaviour, UIUtils.ISingleTargetWindow
    {
        // Config
        [Header("Config")] [SerializeField] private GameObject resourceDetailPanel;

        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text resourceTypeText;
        [SerializeField] private Image resourceTypeIcon;
        [SerializeField] private TMP_Text resourceAmountText;

        [SerializeField] private TMP_Text remainingTime;
        [SerializeField] private GameObject regeneratingGo;

        // Interface
        public static ResourceDetailWindow Instance;

        public void Show(Vector2? pos = null)
        {
            resourceDetailPanel.SetActive(true);
        }

        public void Hide()
        {
            resourceDetailPanel.SetActive(false);
            regeneratingGo.SetActive(false);
        }

        public bool IsOpened()
        {
            return resourceDetailPanel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<ResourceAttr>(target)) return false;
            _targetEntity = target;
            UpdateStaticInfo();
            UpdateDynamicInfo();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        // Internal Data

        private Entity _targetEntity = Entity.Null;

        // ECS
        private EntityManager _em;
        private EntityQuery _notPauseTag;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            Hide();
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            if (!IsOpened()) return;
            if (_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<GeneralAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }

            UpdateDynamicInfo();
        }

        private void UpdateStaticInfo()
        {
            var generalAttr = _em.GetComponentData<GeneralAttr>(_targetEntity);
            var resourceAttr = _em.GetComponentData<ResourceAttr>(_targetEntity);
            resourceTypeIcon.sprite = BasicResourceManager.Instance.ResourceSprites[resourceAttr.Type];
            resourceTypeText.text = resourceAttr.Type.ToString();
            resourceAmountText.text = resourceAttr.AmountRange.lower + " - " + resourceAttr.AmountRange.upper;
            descriptionText.text = DatabaseManager.ResourceDatabaseSo.GetItemById(generalAttr.ID).description;
        }

        private void UpdateDynamicInfo()
        {
            if (_em.HasComponent<RegeneratingTag>(_targetEntity))
            {
                var renewableResourceData = _em.GetComponentData<RenewableData>(_targetEntity);
                regeneratingGo.SetActive(true);
                remainingTime.enabled = true;
                remainingTime.text = UIMathMethods.FormatTime((int)renewableResourceData.RegeneratingLeftTime);
            }
            else
            {
                regeneratingGo.SetActive(false);
                remainingTime.enabled = false;
            }
        }
    }
}
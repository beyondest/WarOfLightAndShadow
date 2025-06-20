using System;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupDetailWindow : MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {

        [SerializeField] private GameObject panel;
        [SerializeField] private Image armyGroupIcon;
        
        public static ArmyGroupDetailWindow Instance;
       
        
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
            if (!_em.HasComponent<ArmyGroupAttr>(target))
            {
                return false;
            }
            _targetEntity = target;
            UpdateStaticData();
            return true;
        }

        public bool HasTarget()
        {
            return _targetEntity != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _targetEntity = Entity.Null;
        }

        private Entity _targetEntity;
        private EntityManager _em;
        
        
        #region EventFunctions

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            Hide();
        }

        
        #endregion


        private void UpdateStaticData()
        {
            var armyGroupAttr = _em.GetComponentData<ArmyGroupAttr>(_targetEntity);
            armyGroupIcon.sprite = ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.IconType];
        }
        
    }
}
using System;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Resource;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class ResourceDetailWindow : MonoBehaviour,UIUtils.ISingleTargetWindow
    {

        [SerializeField] private GameObject panel;
        
        // Interface
        public static ResourceDetailWindow Instance;
        public void Show(Vector2? pos = null)
        {
            panel.SetActive(true);
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasComponent<ResourceAttr>(target)) return false;
            _targetEntity = target;
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
            if(Instance == null)
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
            if(_notPauseTag.IsEmpty)return;
            if(!IsOpened())return;
            if(_targetEntity == Entity.Null) return;
            if (!_em.HasComponent<InteractableAttr>(_targetEntity))
            {
                _targetEntity = Entity.Null;
                return;
            }
            UpdateResourceInfo();
        }

        private void UpdateResourceInfo()
        {
            
        }
    }
}
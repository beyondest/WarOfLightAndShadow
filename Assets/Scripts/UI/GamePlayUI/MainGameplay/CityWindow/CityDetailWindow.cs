using System;
using GamePlaySystem.Functionality.MainGameplay.City;
using GamePlaySystem.Functionality.MainGameplay.General;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class CityDetailWindow :MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {
        #region Config

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
        }

        public bool IsOpened()
        {
           return panel.activeSelf;
        }

        public bool TrySwitchTarget(Entity target)
        {
            throw new System.NotImplementedException();
        }

        public bool HasTarget()
        {
            throw new System.NotImplementedException();
        }

        public void ClearCloseUpTarget()
        {
            throw new System.NotImplementedException();
        }

        #endregion

        #region ButtonMethods

        public void OnClickEnterCity()
        {
            var cityAttr = _em.GetComponentData<CityAttr>(_targetEntity);
            
        }
        

        #endregion


       
        // Internal Data
        private Entity _targetEntity;
        private EntityManager _em;
        
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
        }

        #endregion

    }
    
    
}
using System;
using System.Collections.Generic;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.GamePlay
{
    public class ConjureWindow : UIUtils.MultiSlotsWindow<UnitConjureSlot>
    {


        public static ConjureWindow Instance;
        [NonSerialized] public bool InitWindowEvents = false;
        public Action<Entity, int> EcsConjureUnits;


        #region ButtonMethods

        public override void OnClickSlot(int slotIndex)
        {
            
        }

        #endregion
        
        private readonly List<Sprite> _sprites = new List<Sprite>();
        private readonly List<Entity> _entities = new List<Entity>();
        
        
        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void UpdateUnitCandidates()
        {
            if(!UnitWindowResourceManager.Instance.IsResourceLoaded())return;
            _sprites.Clear();
            _entities.Clear();
            
        }
        
        
        
    }
}
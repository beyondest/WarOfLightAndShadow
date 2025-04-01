using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.General;
using SparFlame.UI.General;
using UnityEngine;

namespace SparFlame.UI.GamePlay.BuffWindows
{
    public class CloseUpWindowResourceManager : CustomResourceManager
    {

        public static CloseUpWindowResourceManager Instance;
        public Dictionary<Tier, Sprite> Tier2Sprites;
        
        
        public override bool IsResourceLoaded()
        {
            throw new System.NotImplementedException();
        }

        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }
    }
}
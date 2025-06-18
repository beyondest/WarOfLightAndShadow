using System;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class MainGameplayCloseUpWindow : MonoBehaviour, MultiSlotWindowUtils.ISingleTargetWindow
    {

        public static MainGameplayCloseUpWindow Instance;

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        public void Show(Vector2? pos = null)
        {
            throw new System.NotImplementedException();
        }

        public void Hide()
        {
            throw new System.NotImplementedException();
        }

        public bool IsOpened()
        {
            throw new System.NotImplementedException();
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
    }
}
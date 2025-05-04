using System;
using UnityEngine;

namespace SparFlame.UI.GamePlay.UI.GamePlayUI.MiniMap
{
    public class MiniMapWindow : MonoBehaviour
    {
        public RectTransform miniMapRect;
        public RectTransform miniMapSquareRect;

        public static MiniMapWindow Instance;
        public event Action EcsOnSquareDrag ;
        private void Awake()
        {
            if(Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
            }
        }

        public void OnSquareDrag()
        {
            EcsOnSquareDrag?.Invoke();
        }
    }
}
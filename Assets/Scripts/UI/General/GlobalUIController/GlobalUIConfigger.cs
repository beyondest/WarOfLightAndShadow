using UnityEngine;

namespace SparFlame.UI.General
{
    public class GlobalUIConfigger : MonoBehaviour
    {
        
        public float doubleClickThreshold = 0.3f; // seconds
        public float lightGeneralFactionAlpha = 0.3f;
        public float darkGeneralFactionAlpha = 0.5f;
        public static GlobalUIConfigger Instance;
        
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
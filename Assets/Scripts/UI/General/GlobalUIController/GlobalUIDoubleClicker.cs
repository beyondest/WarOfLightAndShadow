using UnityEngine;

namespace SparFlame.UI.General
{
    public class GlobalUIDoubleClicker : MonoBehaviour
    {
        
        public float doubleClickThreshold = 0.3f; // seconds
        
        public static GlobalUIDoubleClicker Instance;
        
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
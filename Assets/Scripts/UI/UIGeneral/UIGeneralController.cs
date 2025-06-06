using UnityEngine;

namespace SparFlame.UI.General
{
    public class UIGeneralController : MonoBehaviour
    {
        
        public float doubleClickThreshold = 0.3f; // seconds
        
        public static UIGeneralController Instance;
        
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
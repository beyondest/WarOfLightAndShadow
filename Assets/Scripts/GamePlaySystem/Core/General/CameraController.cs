using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public class CameraController : MonoBehaviour
    {
        public Camera subGameCamera;
        public Camera mainGameCamera;

        public GameObject subGameCameraGo;
       public GameObject mainGameCameraGo;
        public static CameraController Instance;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            subGameCameraGo.SetActive(false);
            mainGameCameraGo.SetActive(true);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class UIAlphaHitNotResponse : MonoBehaviour
    {
        void Start()
        {
            GetComponent<Image>().alphaHitTestMinimumThreshold = 0.5f;
        }

        public void TestClick()
        {
            Debug.Log("TestClick");
        }

    }
}
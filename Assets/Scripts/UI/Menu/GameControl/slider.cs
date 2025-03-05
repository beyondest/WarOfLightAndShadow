using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.Menu.GameControl
{
    public class slider : MonoBehaviour
    {
        public Slider S;


        void Update()
        {
            S.value += 1;
        }
    }
}

using UnityEngine;
using TMPro;

namespace SparFlame.UI.Menu.GameControl
{
    public class t : MonoBehaviour
    {
        [SerializeField] TMP_Text text;
        private int _score;
        void Start()
        {
            
        }

        void Update()
        {
            _score = _score + 1;
            text.text = _score.ToString();
        }
    }
}

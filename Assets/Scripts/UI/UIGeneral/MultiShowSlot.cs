using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public class MultiShowSlot : MonoBehaviour
    {
        public int Index { get; set; }
        [CanBeNull] public Button button;
    }
}
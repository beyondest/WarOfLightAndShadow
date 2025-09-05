using System;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    public static class ButtonUtils
    {
        [RequireComponent(typeof(Button))]
        public class SelfButton : MonoBehaviour
        {
            
            protected virtual void Awake()
            {
                var button = GetComponent<Button>();
                button.onClick.AddListener(OnClick);
            }

            public virtual void OnClick()
            {
                throw new ArgumentException("Not implemented");
            }
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.General
{
    [RequireComponent(typeof(UIBounce))]
    [RequireComponent(typeof(Button))]
    public class UIModalWindow : MonoBehaviour
    {
       private UIBounce _bounce;

       private void Awake()
       {
            _bounce = GetComponent<UIBounce>();
            var button = GetComponent<Button>();
            button.onClick.AddListener(OnBlockerClicked);
       }

       void OnEnable()
        {
            if(GlobalModalWindowManager.Instance)
                GlobalModalWindowManager.Instance.PushWindow(this);
        }

        void OnDisable()
        {
            if(GlobalModalWindowManager.Instance)
                GlobalModalWindowManager.Instance.PopWindow(this);
        }


        public void PlayBounce()
        {
           _bounce.PlayBounce();
        }

        // 遮罩点击时调用
        public void OnBlockerClicked()
        {
            GlobalModalWindowManager.Instance.NotifyClickBlocked();
        }
    }
}
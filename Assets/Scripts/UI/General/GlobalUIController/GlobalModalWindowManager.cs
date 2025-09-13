using System.Collections.Generic;
using UnityEngine;

namespace SparFlame.UI.General
{
    public class GlobalModalWindowManager : MonoBehaviour
    {
        public static GlobalModalWindowManager Instance;

        private Stack<UIModalWindow> windowStack = new();

        private void Awake()
        {
            if (!Instance) Instance = this;
            else Destroy(gameObject);
        }

        public void PushWindow(UIModalWindow window)
        {
            windowStack.Push(window);
            // UpdateTopWindow();
        }

        public void PopWindow(UIModalWindow window)
        {
            if (windowStack.Count > 0 && windowStack.Peek() == window)
            {
                windowStack.Pop();
                // UpdateTopWindow();
            }
            else
            {
                // 如果关的不是栈顶，就简单移除
                var list = new List<UIModalWindow>(windowStack);
                list.Remove(window);
                windowStack = new Stack<UIModalWindow>(list);
                // UpdateTopWindow();
            }
        }

        // private void UpdateTopWindow()
        // {
        //     foreach (var w in windowStack)
        //         w.SetAsTop(false);
        //
        //     if (windowStack.Count > 0)
        //         windowStack.Peek().SetAsTop(true);
        // }

        public void NotifyClickBlocked()
        {
            if (windowStack.Count > 0)
                windowStack.Peek().PlayBounce();
        }
    }
}
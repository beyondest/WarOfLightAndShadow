using System;
using SparFlame.Components.MainGameplay;
using SparFlame.UI.General;
using SparFlame.UI.SubGameplay.StaticWindows;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupAddTypeSelectWindow : MonoBehaviour,MultiSlotWindowUtils.IUIWindow
    {
        [SerializeField] private GameObject panel;

        public static ArmyGroupAddTypeSelectWindow Instance;
        
        public event Action OnEcsGiveUpAndOnlySelectNoArmyGroupUnits;
        public event Action OnEcsGiveUpAndOnlySelectExistArmyGroupUnits;
        private Entity _target;
        public void Show(Vector2? pos = null)
        {
            panel.SetActive(true);
        }
        public void SetTarget(Entity target)
        {
            _target = target;
        }

        public void Hide()
        {
            panel.SetActive(false);
        }

        public bool IsOpened()
        {
            return panel.activeSelf;
        }


        public void OnClickBeginAddAndOverrideExistArmyGroup()
        {
            ArmyGroupSlotWindow.Instance.OnEcsAddToArmyGroup?.Invoke(_target, AddToArmyGroupType.AllSelectedOverrideAlreadyIn);
            Hide();
        }

        public void OnClickBeginAddAndExceptExistArmyGroup()
        {
            ArmyGroupSlotWindow.Instance.OnEcsAddToArmyGroup?.Invoke(_target, AddToArmyGroupType.AllSelectedExceptAlreadyIn);
            Hide();
        }

        public void OnClickGiveUpAndOnlySelectNoArmyGroupUnits()
        {
            OnEcsGiveUpAndOnlySelectNoArmyGroupUnits?.Invoke();
            Hide();
        }

        public void OnClickGiveUpAndOnlySelectExistArmyGroupUnits()
        {
            OnEcsGiveUpAndOnlySelectExistArmyGroupUnits?.Invoke();
            Hide();
        }

        public void Return()
        {
            Hide();
        }

        private void Awake()
        {
            if(!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            Hide();
        }
    }
}
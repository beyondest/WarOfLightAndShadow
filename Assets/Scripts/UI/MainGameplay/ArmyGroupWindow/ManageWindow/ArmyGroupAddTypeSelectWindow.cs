using System;
using SparFlame.Components.MainGameplay;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupAddTypeSelectWindow : MonoBehaviour,MultiSlotWindowUtils.IUIWindow
    {
        [SerializeField] private GameObject panel;

        public static ArmyGroupAddTypeSelectWindow Instance;
        
        public Action OnEcsGiveUpAndOnlySelectNoArmyGroupUnits;
        public Action OnEcsGiveUpAndOnlySelectExistArmyGroupUnits;
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
            ArmyGroupManageWindow.Instance.OnEcsAddToArmyGroup?.Invoke(_target, AddToArmyGroupType.AllSelectedOverrideAlreadyIn);
            Hide();
            ArmyGroupManageWindow.Instance.Hide();
        }

        public void OnClickBeginAddAndExceptExistArmyGroup()
        {
            ArmyGroupManageWindow.Instance.OnEcsAddToArmyGroup?.Invoke(_target, AddToArmyGroupType.AllSelectedExceptAlreadyIn);
            Hide();
            ArmyGroupManageWindow.Instance.Hide();
        }

        public void OnClickGiveUpAndOnlySelectNoArmyGroupUnits()
        {
            OnEcsGiveUpAndOnlySelectNoArmyGroupUnits?.Invoke();
            Hide();
            ArmyGroupManageWindow.Instance.Hide();
        }

        public void OnClickGiveUpAndOnlySelectExistArmyGroupUnits()
        {
            OnEcsGiveUpAndOnlySelectExistArmyGroupUnits?.Invoke();
            Hide();
            ArmyGroupManageWindow.Instance.Hide();
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
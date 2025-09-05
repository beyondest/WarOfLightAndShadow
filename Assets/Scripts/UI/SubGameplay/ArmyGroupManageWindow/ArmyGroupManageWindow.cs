using System;
using System.Collections.Generic;
using SparFlame.Components.MainGameplay;
using SparFlame.UI.General;
using TMPro;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    public class ArmyGroupManageWindow : MultiSlotWindowUtils.MultiSlotsWindow<ArmyGroupManageInfoSlot>
    {
        // Config

        [SerializeField] private GameObject totalPanel;

        [SerializeField] private TMP_Text armyGroupAllowedCountText;
        // Interface
        public static ArmyGroupManageWindow Instance;
        public event Action OnEcsUpdateStaticData;
        public event Action<Entity> OnEcsDeleteArmyGroup;
        public Action<Entity, AddToArmyGroupType> OnEcsAddToArmyGroup;
        public event Action OnEcsCheckSelected;
        public event Action<int, int> OnEcsTryNewArmyGroup;
        public event Action OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned;
        public bool HasSelectedUnitAlreadyInArmyGroup { get; set; }

        public void UpdateStaticData(List<ArmyGroupManageInfo> infos,  int maxGarrisonCount)
        {
            _maxGarrisonCount = maxGarrisonCount;
            _infos.Clear();
            _infos.AddRange(infos);
            for (var i = 0; i < Slots.Count; i++)
            {
                var slot = Slots[i];
                var slotComponent = SlotComponents[i];
                if (i < infos.Count)
                {
                    slotComponent.SetTarget(infos[i]);
                    slot.SetActive(true);
                }
                else
                {
                    slot.SetActive(false);
                }
            }
            armyGroupAllowedCountText.text = _infos.Count + "/" +maxGarrisonCount ;
        }

        public void DeleteArmyGroup(int index)
        {
            var armyGroup = _infos[index].ArmyGroupEntity;
            OnEcsDeleteArmyGroup?.Invoke(armyGroup);
            UpdateStaticData(_infos,_maxGarrisonCount);
        }

        public void AddToArmyGroup(int index)
        {
            OnEcsCheckSelected?.Invoke();

            if (HasSelectedUnitAlreadyInArmyGroup)
            {
                ArmyGroupAddTypeSelectWindow.Instance.Show();
                ArmyGroupAddTypeSelectWindow.Instance.SetTarget(_infos[index].ArmyGroupEntity);
            }
            else
            {
                OnEcsAddToArmyGroup?.Invoke(_infos[index].ArmyGroupEntity,
                    AddToArmyGroupType.AllSelectedExceptAlreadyIn);
            }
        }

        public override void Show(Vector2? pos = null)
        {
            base.Show(pos);
            totalPanel.SetActive(true);
        }

        public override void Hide()
        {
            base.Hide();
            totalPanel.SetActive(false);
        }

        public override bool IsOpened()
        {
            return totalPanel.activeSelf;
        }

        #region ButtonMethods

        public void OnClickOpenArmyGroupManageWindow()
        {
            OnEcsUpdateStaticData?.Invoke();
            Show();
        }

        public void OnClickCloseArmyGroupManageWindow()
        {
            // var hasEmptyArmyGroup = false;
            // OnEcsUpdateStaticData?.Invoke();
            // foreach (var info in _infos)
            // {
            //     if (info.TotalUnitCount == 0)
            //     {
            //         hasEmptyArmyGroup = true;
            //         break;
            //     }
            // }
            // if (hasEmptyArmyGroup)
            // {
            //     ConfirmWindow.Instance.Show("You have army group with no units. Close the window will delete the army group.",
            //         () =>
            //         {
            //             var deleteInfos = new List<ArmyGroupManageInfo>();
            //             foreach (var info in _infos)
            //             {
            //                 if (info.TotalUnitCount == 0)
            //                     deleteInfos.Add(info);
            //             }
            //
            //             foreach (var info in deleteInfos)
            //             {
            //                 OnEcsDeleteArmyGroup?.Invoke(info.ArmyGroupEntity);
            //             }
            //             Hide();
            //         });
            // }
            // else
            // {
            //
            // }
            Hide();

        }
 
        public void OnClickNewArmyGroup()
        {
            OnEcsTryNewArmyGroup?.Invoke(_infos.Count, config.rows * config.cols);
        }

        public void OnClickSelectAllUnitsWithoutArmyGroupAndGarrisoned()
        {
            OnEcsSelectAllUnitsWithoutArmyGroupAndGarrisoned?.Invoke();
        }
        #endregion

        private readonly List<ArmyGroupManageInfo> _infos = new();
        private EntityManager _em;
        private int _maxGarrisonCount;
        
        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            Hide();
        }
    }

    public struct ArmyGroupManageInfo
    {
        public Entity ArmyGroupEntity;
        public int TotalUnitCount;
        // 
        // public ArmyGroupIconType IconType;
        // public string GameplayName;
        // // Info
        // public float Speed;
        // public float Morale;
        //     
        // // Composition
        // public int TotalUnitCount;
        // public DynamicBuffer<ArmyGroupUnitTypeData> TypeDatas;
    }
}
using System.Collections.Generic;
using NUnit.Framework;
using SparFlame.Components.General;
using SparFlame.Components.MainGameplay;
using SparFlame.Systems.General.BasicControl;
using SparFlame.UI.General;
using Unity.Entities;

namespace SparFlame.UI.MainGameplay
{
    public class CityGarrisonWindow : MultiSlotWindowUtils.MultiSlotsWindow<CityGarrisonSlot>,
        MultiSlotWindowUtils.ISingleTargetWindow
    {
        public static CityGarrisonWindow Instance;

        public bool TrySwitchTarget(Entity target)
        {
            if (!_em.HasBuffer<CityGarrisonEntity>(target)) return false;
            var buffer = _em.GetBuffer<CityGarrisonEntity>(target);
            if (buffer.Length == 0) return false;
            _target = target;
            UpdateSlots();
            return true;
        }

        public bool HasTarget()
        {
            return _target != Entity.Null;
        }

        public void ClearCloseUpTarget()
        {
            _target = Entity.Null;
        }

        public override void OnClickSlot(int slotIndex)
        {
            var generalAttr = _em.GetComponentData<MainGameplayGeneralAttr>(_target);
            var playerFactionData = _em.CreateEntityQuery(typeof(PlayerFactionData)).GetSingleton<PlayerFactionData>();
            var relationship = FactionUtils.GetRelationship(playerFactionData,generalAttr.faction,generalAttr.subFaction);
            if(relationship != Relationship.Player)return;
            
            var buffer = _em.GetBuffer<CityGarrisonEntity>(_target);
            if (buffer.Length == 0 || slotIndex >= buffer.Length) return;

            var nonZeroList = new List<CityGarrisonEntity>();
            foreach (var cityGarrisonEntity in buffer)
            {
                var units = _em.GetBuffer<ArmyGroupUnit>(cityGarrisonEntity.ArmyGroup);
                if(units.Length == 0)continue;
                nonZeroList.Add(cityGarrisonEntity);
            }
            if(slotIndex >= nonZeroList.Count) return;
            
            var request = _em.CreateEntity();
            _em.AddComponent<ArmyGroupGarrisonRequest>(request);
            _em.SetComponentData(request, new ArmyGroupGarrisonRequest
            {
                City = _target,
                ArmyGroup = nonZeroList[slotIndex].ArmyGroup,
                IfGarrisonIn = false
            });
        }

        private EntityManager _em;
        private Entity _target;
        private EntityQuery _mainGamingTag;

        #region EventFunctions

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
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _mainGamingTag = _em.CreateEntityQuery(typeof(MainGamingTag));
            Hide();
        }

        private void Update()
        {
            if (!IsOpened() || _mainGamingTag.IsEmpty) return;
            if (!HasTarget()) return;
            if (!_em.HasBuffer<CityGarrisonEntity>(_target))
            {
                _target = Entity.Null;
                Hide();
                return;
            }
            UpdateSlots();
        }

        #endregion

        private void UpdateSlots()
        {
            var oriBuffer = _em.GetBuffer<CityGarrisonEntity>(_target);
            var noZeroBuffer = new List<CityGarrisonEntity>();
            foreach (var cityGarrisonEntity in oriBuffer)
            {
                var units = _em.GetBuffer<ArmyGroupUnit>(cityGarrisonEntity.ArmyGroup);
                if(units.Length == 0)continue;
                noZeroBuffer.Add(cityGarrisonEntity);
            }
       

            for (var i = 0; i < Slots.Count; i++)
            {
                if (i < noZeroBuffer.Count)
                {
                    var armyGroup = noZeroBuffer[i].ArmyGroup;
                    var armyGroupAttr = _em.GetComponentData<ArmyGroupAttr>(armyGroup);
                    
                    Slots[i].SetActive(true);
                    
                    SlotComponents[i].button!.image.sprite =
                        ArmyGroupWindowResourceManager.Instance.ArmyGroupIcons[armyGroupAttr.iconType];
                    SlotComponents[i].armyGroupName.text = armyGroupAttr.gameplayName.ToString();
                }
                else
                {
                    Slots[i].SetActive(false);
                }
            }
        }
    }
}
using System;
using SparFlame.Components.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class SaveLoadController : MonoBehaviour
    {
        public static SaveLoadController Instance;

        // Load actions
        public Action OnEcsLoadArmyGroupSubData;
        public Action<SubGameStatusData> OnEcsLoadCitySubData;
        public Action<int> OnEcsLoadGeneralGameData;
        
        // Save actions
        public Action OnEcsSaveCitySubData;
        public Action OnEcsSaveArmyGroupSubData;
        public Action OnEcsSaveArmyGroupMainData;
        public Action OnEcsSaveCityMainData;
        
        public void SyncSaveGame(bool forceSaveAll = false)
        {
            var subGameStatusData = _currentSubGameStatusQuery.GetSingleton<SubGameStatusData>();
            if(subGameStatusData.IsInBattle)return; // When in battle, saving is not allowed
            if (forceSaveAll)
            {
                if(subGameStatusData.SubGameStatus == SubGameStatus.PlayerCity)
                    OnEcsSaveCitySubData?.Invoke();
                OnEcsSaveArmyGroupSubData?.Invoke();
                OnEcsSaveArmyGroupMainData?.Invoke();
                OnEcsSaveCityMainData?.Invoke();
                return;
            }
            
            // If battle not complete, save game is not allowed, player only has the pre-battle saving;
            // If battle complete but player failed, city sub game data still not save, because now city does not belong to player;
            // If battle complete and player win, city sub game data will be saved;
            
            switch (subGameStatusData.SubGameStatus)
            {
                // This happens when player save in his city or after win the battle
                case SubGameStatus.PlayerCity:
                    OnEcsSaveCitySubData?.Invoke();
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    break;
                // This happens when player save in the main world
                case SubGameStatus.None:
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    break;
                // This happens when player failed the siege battle
                case SubGameStatus.PlayerSiege:
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    break;
                // This happens when player failed the defend battle
                case SubGameStatus.PlayerDefend:
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    break;
                // This happens when player failed the encounter battle
                case SubGameStatus.Encounter:
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    break;
                // This happens when player failed the support battle
                case SubGameStatus.Support:
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    break;
            }
        }

        public void LoadGeneralGameData()
        {
            OnEcsLoadGeneralGameData?.Invoke(_saveSlot);
        }

        public void SyncLoadSubGameData(SubGameStatusData targetSubGameStatusData)
        {
            switch (targetSubGameStatusData.SubGameStatus)
            {
                case SubGameStatus.PlayerCity:
                case SubGameStatus.PlayerDefend:
                    OnEcsLoadCitySubData?.Invoke(targetSubGameStatusData);
                    OnEcsLoadArmyGroupSubData?.Invoke();
                    break;
                case SubGameStatus.None:
                    // This should never happen
                    break;
                case SubGameStatus.PlayerSiege:
                case SubGameStatus.Encounter:
                case SubGameStatus.Support:
                    OnEcsLoadArmyGroupSubData?.Invoke();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private int _saveSlot;
        private EntityQuery _currentSubGameStatusQuery;
        private EntityManager _em;

        #region EventFunctions
        
        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void Start()
        {
            _currentSubGameStatusQuery =
                World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(typeof(SubGameStatusData));
        }
        
        #endregion

        // private void LoadCityGarrisonArmyGroups(Entity city)
        // {
        //     var garrisonEntities = _em.GetBuffer<CityGarrisonEntity>(city);
        //     foreach (var garrisonEntity in garrisonEntities)
        //     {
        //         OnEcsLoadArmyGroupData?.Invoke(garrisonEntity.ArmyGroup);
        //     }
        // }
        //
        // private void LoadInsightArmyGroups(Entity armyGroup)
        // {
        //     var sightTarget = _em.GetBuffer<ArmyGroupSightTarget>(armyGroup);
        //     var generalAttr = _em.GetComponentData<MainGameplayGeneralAttr>(armyGroup);
        //     foreach (var armyGroupSightTarget in sightTarget)
        //     {
        //         var insightArmyGroup = armyGroupSightTarget.Entity;
        //         if (generalAttr.Faction == _em.GetComponentData<MainGameplayGeneralAttr>(insightArmyGroup).Faction)
        //         {
        //             OnEcsLoadArmyGroupData?.Invoke(insightArmyGroup);
        //         }
        //     }
        // }
    }
}
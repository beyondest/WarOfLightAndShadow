using System;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class SaveLoadController : MonoBehaviour
    {
        public static SaveLoadController Instance;

        // Load actions
        public event Action OnEcsLoadArmyGroupSubData;
        public event Action<SubGameStatusData> OnEcsLoadCitySubData;
        public event Action OnEcsLoadGameMainData;

        public event Action OnEcsLoadMainGameplayData;
        
        // Save actions
        public event Action OnEcsSaveCitySubData;
        public event Action OnEcsSaveArmyGroupSubData;
        public event Action OnEcsSaveArmyGroupMainData;
        public event Action OnEcsSaveCityMainData;
        public event Action OnEcsSaveGameMainData;
        public void SyncSaveGame()
        {
            var subGameStatusData = _currentSubGameStatusQuery.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return; // When in battle, saving is not allowed
            
            // If battle not complete, save game is not allowed, player only has the pre-battle saving;
            // If battle complete but player failed, city sub game data still not save, because now city does not belong to player;
            // If battle complete and player win, city sub game data will be saved;
            
            switch (subGameStatusData.SubGameStatus)
            {
                // This happens when player save in his city or after win the battle
                case SubGameStatus.PlayerCity:
                    OnEcsSaveCityMainData?.Invoke();
                    OnEcsSaveCitySubData?.Invoke();
                    OnEcsSaveArmyGroupSubData?.Invoke();
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveGameMainData?.Invoke();
                    break;
                // This happens when player save in the main world
                case SubGameStatus.None:
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    OnEcsSaveGameMainData?.Invoke();
                    break;
                // These will never happen because player cannot save in battle
                case SubGameStatus.PlayerSiege:
                case SubGameStatus.PlayerDefend:
                case SubGameStatus.Encounter:
                case SubGameStatus.Support:
                default:
                    break;
            }
        }

        public void LoadGameMainData()
        {
            OnEcsLoadGameMainData?.Invoke();
        }

        public void LoadMainGameplayData()
        {
            OnEcsLoadMainGameplayData?.Invoke();
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
                    BurstSafe.UnexpectedEnum(targetSubGameStatusData.SubGameStatus);
                    break;
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
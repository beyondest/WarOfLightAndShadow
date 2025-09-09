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
        public event Action<bool> OnEcsSaveCitySubData;
        public event Action<bool> OnEcsSaveArmyGroupSubData;
        public event Action OnEcsSaveArmyGroupMainData;
        public event Action OnEcsSaveCityMainData;
        public event Action OnEcsSaveGameMainData;

        public event Action OnEcsCopyAndDeleteTmpSubData;
        
        public void SyncSaveGame(bool backToMainWorldAutoSave = false)
        {
            var subGameStatusData = _currentSubGameStatusQuery.GetSingleton<SubGameStatusData>();
            if(GameStatusUtils.IsInBattle(subGameStatusData))return; // When in battle, saving is not allowed
            
            // If battle not complete, save game is not allowed, player only has the pre-battle saving;
            // If battle complete but player failed, city sub game data still not save, because now city does not belong to player;
            // If battle complete and player win, city sub game data will be saved;
            
            switch (subGameStatusData.SubGameStatus)
            {
                // This happens when player manually save in the city
                case SubGameStatus.PlayerCity :
                    if (!backToMainWorldAutoSave)
                    {
                        OnEcsSaveCityMainData?.Invoke();
                        OnEcsSaveCitySubData?.Invoke(false); // Should save to tmp = false
                        OnEcsSaveArmyGroupSubData?.Invoke(false);
                        OnEcsSaveArmyGroupMainData?.Invoke();
                        OnEcsSaveGameMainData?.Invoke();
                    }
                    else
                    {
                        // This happens when player back to main world auto save
                        OnEcsSaveCitySubData?.Invoke(true);
                        OnEcsSaveArmyGroupSubData?.Invoke(true);
                    }
                
                    break;
                // This happens when player save in the main world
                case SubGameStatus.None:
                    OnEcsSaveArmyGroupMainData?.Invoke();
                    OnEcsSaveCityMainData?.Invoke();
                    OnEcsSaveGameMainData?.Invoke();
                    OnEcsCopyAndDeleteTmpSubData?.Invoke();
                    
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


    }
}
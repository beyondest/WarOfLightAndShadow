using System;
using System.Collections;
using System.IO;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.Systems.General.BasicControl
{
    public class SaveLoadController : MonoBehaviour
    {
        [SerializeField] private float checkInterval = 0.1f;
        [SerializeField] private int checkFrameCountBeforeCheckSaveComplete = 5;
        [SerializeField] private int checkFrameCountAfterCheckSaveComplete = 5;

        public class Operation : ResourceOperation
        {
            public Operation(IEnumerator routine) : base(routine)
            {
            }
        }

        public static SaveLoadController Instance;

        public event Action<SaveType> OnSaveComplete;
        public event Action<SaveType> OnStartSave;

        public void SetSavingSlotAndDoSomeCleaning(bool isNewGame, int saveSlot)
        {
            var playerSaveSlot = _saveSlotQuery.GetSingletonRW<CurrentSaveSlot>();
            playerSaveSlot.ValueRW.Value = saveSlot;
            var slot = playerSaveSlot.ValueRO.Value;
            var folder = SaveUtilities.GetSaveSlotFolder(slot);
            if (isNewGame)
            {
                SaveUtilities.InitializeSaveSlotFolder(folder, saveSlot);
            }
            else
            {
                // Delete tmp sub data save
                var armyGroupSubDataFolder = SaveUtilities.GetArmyGroupSubDataFolder(slot);
                var citySubDataFolder = SaveUtilities.GetCitySubDataFolder(slot);
                var at = Directory.GetFiles(armyGroupSubDataFolder, "*.tmp", SearchOption.AllDirectories);
                var ct = Directory.GetFiles(citySubDataFolder, "*.tmp", SearchOption.AllDirectories);
                foreach (var file in at)
                    File.Delete(file);
                foreach (var file in ct)
                    File.Delete(file);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="saveType"></param>
        /// <param name="targetSlot">This value is only valid when saveType == Manual</param>
        /// <returns></returns>
        public Operation SaveAsync(SaveType saveType, int targetSlot) => new(SelfSaveAsync(saveType, targetSlot));
        public Operation LoadGameMainDataAsync() => new(SelfLoadGameMainDataAsync());

        public Operation LoadGameSubDataAsync(SubGameStatusData targetSubGameStatusData) =>
            new(SelfLoadGameSubDataAsync(targetSubGameStatusData));

    

        #region Internal Callbacks or Events

        // Load actions
        internal event Action OnEcsStartLoadArmyGroupSubData;
        internal event Action<SubGameStatusData> OnEcsStartLoadCitySubData;
        internal event Action OnEcsStartLoadGameMainData;


        // Save actions
        // bool : Should save to tmp. Only sub world to main world save should save to tmp
        internal event Action<bool> OnEcsStartSavingCitySubData;
        // bool : Should save to tmp. Only sub world to main world save should save to tmp
        internal event Action<bool> OnEcsStartSaveArmyGroupSubData;
        internal event Action OnEcsStartSaveGameMainData;
        internal event Action OnEcsStartSaveEnemySpecificArmyGroupSubData;

        // int : Target saving slot. Not current saving slot
        internal event Action<int> OnEcsCopyDeleteTmpSubDatas;

        // int : Target saving slot.
        internal event Action<int> OnEcsCopyOverrideSavingSlot;

        internal event Action OnEcsDeleteInvalidSubDatas;


        internal void OneTaskSaveComplete()
        {
            _stillSaveTaskCount--;
            if (_stillSaveTaskCount == 0)
            {
                IsSaving = false;
                OnSaveComplete?.Invoke(_saveType);
            }
        }

        internal void OneTaskLoadComplete()
        {
            _stillLoadTaskCount--;
            if (_stillLoadTaskCount == 0)
            {
                _isLoading = false;
            }
        }

        #endregion

        private EntityQuery _currentSubGameStatusQuery;
        private EntityQuery _saveSlotQuery;
        private EntityManager _em;
        private SaveType _saveType;
        private int _stillSaveTaskCount;
        private int _stillLoadTaskCount;
        [NonSerialized] public bool IsSaving;
        [NonSerialized] private bool _isLoading;
        
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
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _saveSlotQuery = _em.CreateEntityQuery(typeof(CurrentSaveSlot));
            _currentSubGameStatusQuery =
                _em.CreateEntityQuery(typeof(SubGameStatusData));
        }

        private void OnDestroy()
        {
            try
            {
                if(_saveSlotQuery != default)
                    _saveSlotQuery.Dispose();
                if(_currentSubGameStatusQuery != default)
                    _currentSubGameStatusQuery.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        #endregion
        private IEnumerator WaitForExtraFrames(int waitFrameCount)
        {
            var c = 0;
            while (c < waitFrameCount)
            {
                c += 1;
                yield return null;
            }
        }
        #region Start Save/Load Methods

        private void StartSaveGame(SaveType saveType, int targetSlot)
        {
            _saveType = saveType;
            IsSaving = true;
            OnStartSave?.Invoke(saveType);
            var subGameStatusData = _currentSubGameStatusQuery.GetSingleton<SubGameStatusData>();
            switch (saveType)
            {
                // These save types will clear all tmp files and change current save slot singleton
                case SaveType.Manual:
                case SaveType.Automatic:
                    var currentSaveSlot = _saveSlotQuery.GetSingletonRW<CurrentSaveSlot>();
                    var actualTargetSlot =
                        saveType == SaveType.Automatic ? SaveUtilities.AutomaticSaveSlot : targetSlot;

                    if (currentSaveSlot.ValueRO.Value != actualTargetSlot)
                        OnEcsCopyOverrideSavingSlot?.Invoke(actualTargetSlot);
                    OnEcsCopyDeleteTmpSubDatas?.Invoke(actualTargetSlot);
                    // Switch saving slot
                    currentSaveSlot.ValueRW.Value = actualTargetSlot;

                    switch (subGameStatusData.SubGameStatus)
                    {
                        case SubGameStatus.PlayerCity:
                            _stillSaveTaskCount = 3;
                            OnEcsStartSavingCitySubData?.Invoke(false);
                            OnEcsStartSaveArmyGroupSubData?.Invoke(false);
                            OnEcsStartSaveGameMainData?.Invoke();
                            break;
                        // This happens when player save in the main world
                        case SubGameStatus.None:
                            _stillSaveTaskCount = 1;
                            OnEcsStartSaveGameMainData?.Invoke();
                            break;
                        // When battle end auto save
                        case SubGameStatus.Encounter:
                            _stillSaveTaskCount = 2;
                            OnEcsStartSaveArmyGroupSubData?.Invoke(false);
                            OnEcsStartSaveGameMainData?.Invoke();
                            break;
                        // When battle end auto save
                        case SubGameStatus.PlayerDefend:
                        case SubGameStatus.PlayerSiege:
                        case SubGameStatus.Support:
                            _stillSaveTaskCount = 3;
                            OnEcsStartSavingCitySubData?.Invoke(false);
                            OnEcsStartSaveArmyGroupSubData?.Invoke(false);
                            OnEcsStartSaveGameMainData?.Invoke();
                            break;
                        default:
                            BurstSafe.UnexpectedEnum(subGameStatusData.SubGameStatus);
                            break;
                    }

                    // Clear all invalid sub datas/tmp sub datas(when army group died or city faction change)
                    OnEcsDeleteInvalidSubDatas?.Invoke();
                    break;

                // This save type is made for fast save in current slot
                case SaveType.SaveSubGameplayDataToTmp:
                    switch (subGameStatusData.SubGameStatus)
                    {
                        case SubGameStatus.PlayerCity:
                        case SubGameStatus.PlayerDefend:
                            _stillSaveTaskCount = 2;
                            OnEcsStartSaveArmyGroupSubData?.Invoke(true);
                            OnEcsStartSavingCitySubData?.Invoke(true);
                            break;
                        case SubGameStatus.PlayerSiege:
                        case SubGameStatus.Encounter:
                            _stillSaveTaskCount = 1;
                            OnEcsStartSaveArmyGroupSubData?.Invoke(true);
                            break;
                        // This will never happen
                        case SubGameStatus.Support:
                        case SubGameStatus.None:
                        default:
                            BurstSafe.UnexpectedEnum(saveType);
                            break;
                    }

                    break;

                case SaveType.SaveEnemySpecificArmyGroupSubData:
                    _stillSaveTaskCount = 1;
                    OnEcsStartSaveEnemySpecificArmyGroupSubData?.Invoke();
                    break;
                default:
                    BurstSafe.UnexpectedEnum(saveType);
                    break;
            }

            // If battle not complete, save game is not allowed, player only has the pre-battle saving;
            // If battle complete but player failed, city sub game data still not save, because now city does not belong to player;
            // If battle complete and player win, city sub game data will be saved;
        }

        private void StartLoadGameMainData()
        {
            _isLoading = true;
            _stillLoadTaskCount += 1;
            OnEcsStartLoadGameMainData?.Invoke();
        }


        private void StartLoadGameSubData(SubGameStatusData targetSubGameStatusData)
        {
            _isLoading = true;
            switch (targetSubGameStatusData.SubGameStatus)
            {
                case SubGameStatus.PlayerCity:
                case SubGameStatus.PlayerDefend:
                    _stillLoadTaskCount += 1;
                    OnEcsStartLoadArmyGroupSubData?.Invoke();
                    StartCoroutine(CheckLoadArmyGroupSubDataCompleteAndStartLoadCitySubData(targetSubGameStatusData));
                    break;
                case SubGameStatus.PlayerSiege:
                case SubGameStatus.Encounter:
                case SubGameStatus.Support:
                    _stillLoadTaskCount += 1;
                    OnEcsStartLoadArmyGroupSubData?.Invoke();
                    break;

                case SubGameStatus.None:
                default:
                    BurstSafe.UnexpectedEnum(targetSubGameStatusData.SubGameStatus);
                    break;
            }
        }

        #endregion


        /// <summary>
        /// If multiple save command is called at the same time, will save it one by one
        /// </summary>
        /// <param name="saveType"></param>
        /// <param name="targetSlot"></param>
        /// <returns></returns>
        private IEnumerator SelfSaveAsync(SaveType saveType, int targetSlot)
        {
            yield return WaitForExtraFrames(checkFrameCountBeforeCheckSaveComplete);
            if (IsSaving)
                yield return CheckSavingComplete();
            yield return WaitForExtraFrames(checkFrameCountAfterCheckSaveComplete);
            if(IsSaving)
                yield return CheckSavingComplete();
            StartSaveGame(saveType, targetSlot);
            yield return CheckSavingComplete();
        }

        /// <summary>
        /// If multiple load command is called at the same time, will load it async, but complete until all load complete
        /// </summary>
        /// <returns></returns>
        private IEnumerator SelfLoadGameMainDataAsync()
        {
            StartLoadGameMainData();
            yield return CheckLoadComplete();
        }

        private IEnumerator SelfLoadGameSubDataAsync(SubGameStatusData targetSubGameStatusData)
        {
            StartLoadGameSubData(targetSubGameStatusData);
            yield return CheckLoadComplete();
        }
        
        private IEnumerator CheckSavingComplete()
        {
            while (IsSaving)
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
        }

        private IEnumerator CheckLoadComplete()
        {
            while (_isLoading)
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
            yield return new WaitForSecondsRealtime(checkInterval); // Wait for new job to load
            while (_isLoading)
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
        }

        private IEnumerator CheckLoadArmyGroupSubDataCompleteAndStartLoadCitySubData(SubGameStatusData targetSubGameStatusData)
        {
            while (_isLoading)
            {
                yield return new WaitForSecondsRealtime(checkInterval);
            }
            _isLoading = true;
            _stillLoadTaskCount += 1;
            OnEcsStartLoadCitySubData?.Invoke(targetSubGameStatusData);
        }
    }
}
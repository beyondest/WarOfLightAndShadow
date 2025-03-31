using System;
using System.Collections.Generic;
using SparFlame.GamePlaySystem.Building;
using SparFlame.GamePlaySystem.Building.GamePlaySystem.Core.Object.Building;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.UnitSelection;
using SparFlame.UI.General;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace SparFlame.UI.GamePlay.BuildingWindows
{
    public class ConstructWindow : UIWindow
    {
        public static ConstructWindow Instance;
        
        [Header("Custom config")]
        public GameObject constructPanel;
        public KeyCode exitGhostModeKey = KeyCode.Escape;
        public List<BuildingIndexSpritePair> buildingIndexSpritePairs;
        
        [Header("Multi building slot config")]
        public GameObject buildingSlotPrefab;
        public MultiShowSlotConfig multiShowSlotConfig;
        
        public void OnClickBuildingImage(int index)
        {
            SwitchTargetBuilding(_type2Entites[(BuildingType)_currentPage][index]);
        }

        public void OnClickBuildingTypeHeader(BuildingType buildingType)
        {
            _currentPage = (int)buildingType;
            // UpdateBuildingCandidates();
        }
        
        
        public override void Show(Vector2? pos = null)
        {
            constructPanel.SetActive(true);
        }

        public override void Hide()
        {
            constructPanel.SetActive(false);
        }

        public override bool IsOpened()
        {
            return constructPanel.activeSelf;
        }


        /// <summary>
        /// Only show ghost building when click on some building icons in construct panel
        /// Or click on the building movement icon in building detail panel
        /// </summary>
        private bool _showGhostBuilding;
        private int _slotsMaxCountPerPage;
        private int _currentPage;
        private FactionTag _factionTag;
        private Dictionary<BuildingType, List<Entity>> _type2Entites = new();
        private Dictionary<BuildingType, List<Sprite>> _type2Sprites = new();
        private List<GameObject> _buildingSlots = new();
        
        
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _placementCommandData;
        private EntityQuery _unitSelectionData;
        private EntityQuery _customInputData;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }


        private void Start()
        {
            _em = World.DefaultGameObjectInjectionWorld.EntityManager;
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _placementCommandData = _em.CreateEntityQuery(typeof(PlacementCommandData));
            _customInputData = _em.CreateEntityQuery(typeof(CustomInputSystemData));
            _unitSelectionData = _em.CreateEntityQuery(typeof(UnitSelectionData));

            foreach (var type in Enum.GetValues(typeof(BuildingType)))
            {
                _type2Entites.Add((BuildingType)type, new List<Entity>());
                _type2Sprites.Add((BuildingType)type, new List<Sprite>());
            }
            // Init index to sprites
            foreach (var pair in buildingIndexSpritePairs)
            {
                _type2Sprites[pair.type].Add(pair.sprite);
            }
            // Init index to entity
            if (!_em.CreateEntityQuery(typeof(BuildingIndexPrefab))
                .TryGetSingletonBuffer(out DynamicBuffer<BuildingIndexPrefab> database))
            {
                throw new ArgumentException("Construct window init wrong");
            }
            foreach (var pair in database)
            {
                _type2Entites[pair.Type].Add(pair.Prefab);
            }
            if(_type2Entites.Count != _type2Sprites.Count)
                throw new ArgumentException("Construct window init wrong");
            
            // Init multi slots
            UIUtils.InitMultiShowSlots(ref _buildingSlots, constructPanel,buildingSlotPrefab,
                in multiShowSlotConfig,OnClickBuildingImage);
            if (_buildingSlots.Count < _type2Entites.Count)
                throw new ArgumentException("Construct window init wrong");
            
            _slotsMaxCountPerPage = multiShowSlotConfig.rows * multiShowSlotConfig.cols;
            Hide();

        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;
            var inputData = _customInputData.GetSingleton<CustomInputSystemData>();
            var unitSelectionData = _unitSelectionData.GetSingleton<UnitSelectionData>();
            _factionTag = unitSelectionData.CurrentSelectFaction;
            if (!_showGhostBuilding) return;

            if (inputData is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left, IsOverUI: false })
            {
                var datas = _placementCommandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
                var data = new PlacementCommandData();
                for (var i = 0; i < datas.Length; i++)
                {
                    if (datas[i].Faction != _factionTag) // This may happen when enemy is in ghost mode with player
                        continue;
                    data = datas[i];
                }

                switch (data.State)
                {
                    case PlacementStateType.Valid:
                        BuildTarget();
                        break;
                    case PlacementStateType.Overlapping:
                    case PlacementStateType.NotEnoughResources:
                    case PlacementStateType.NotConstructable:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (Input.GetKeyDown(exitGhostModeKey))
            {
                ExitGhostShow();
            }
            
        }

        
        // private void UpdateBuildingCandidates()
        // {
        //     var startIdx = _currentPage * _slotsMaxCountPerPage;
        //     var count = Mathf.Min(_slotsMaxCountPerPage, selectedUnitInfo.Count - startIdx);
        //     // Update corresponding images and hp sliders
        //     for (var i = 0; i < _slotsMaxCountPerPage; i++)
        //     {
        //         if (i < count)
        //         {
        //             _slots[i].SetActive(true);
        //             var unitShowSlot = _slots[i].GetComponent<UnitMulti2DSlot>();
        //             var unitInfo = selectedUnitInfo[startIdx + i];
        //             unitShowSlot.button.image.sprite = _unitSpriteDict[unitInfo.UnitType];
        //             unitShowSlot.hp.value = unitInfo.HpRatio;
        //         }
        //         else
        //         {
        //             _slots[i].SetActive(false);
        //         }
        //     }
        //
        //     // Update right and left button
        //     pageDownButton.SetActive((_currentPage + 1) * _slotsMaxCountPerPage < selectedUnitInfo.Count);
        //     pageUpButton.SetActive(_currentPage != 0);
        // }
        
        #region GhostShow

        private void EnterGhostShow(Entity target)
        {
            var entity = _em.CreateEntity();
            _em.AddComponent<PlacementCommandData>(entity);
            var data = new PlacementCommandData
            {
                TargetBuilding = target,
                CommandType = PlacementCommandType.Start,
                Faction = _factionTag,
                GhostEntity = Entity.Null,
                GhostTriggerEntity = Entity.Null,
                Rotation = quaternion.identity,
                State = PlacementStateType.Valid
            };
            _em.SetComponentData(entity, data);
        }

        private void SwitchTargetBuilding(Entity target)
        {
            if (!_showGhostBuilding)
            {
                EnterGhostShow(target);
                return;
            }

            var entities = _placementCommandData.ToEntityArray(Allocator.Temp);
            var datas = _placementCommandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _factionTag)
                    continue;
                var data = datas[i];
                data.TargetBuilding = target;
                data.CommandType = PlacementCommandType.Start;
                _em.SetComponentData(entities[i], data);
            }
        }

        private void BuildTarget()
        {
            var entities = _placementCommandData.ToEntityArray(Allocator.Temp);
            var datas = _placementCommandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _factionTag)
                    continue;
                var data = datas[i];
                data.CommandType = PlacementCommandType.Build;
                _em.SetComponentData(entities[i], data);
            }
        }

        private void ExitGhostShow()
        {
            var entities = _placementCommandData.ToEntityArray(Allocator.Temp);
            var datas = _placementCommandData.ToComponentDataArray<PlacementCommandData>(Allocator.Temp);
            for (var i = 0; i < datas.Length; i++)
            {
                if (datas[i].Faction != _factionTag)
                    continue;
                var data = datas[i];
                data.CommandType = PlacementCommandType.End;
                _em.SetComponentData(entities[i], data);
                _showGhostBuilding = false;
            }
        }
        #endregion

        
    }

    [Serializable]
    public struct BuildingIndexSpritePair
    {
        public BuildingType type;
        public Sprite sprite;
    }
    
    
}
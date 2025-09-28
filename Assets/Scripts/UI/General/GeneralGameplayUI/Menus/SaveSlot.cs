using System;
using System.IO;
using SparFlame.Components.General;
using SparFlame.Database;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.BasicControl.GlobalMonos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace SparFlame.UI.General
{
    public class SaveSlot : MultiShowSlot
    {
        [SerializeField] private TMP_Text content;
        [SerializeField] private GameObject deleteButton;
        [SerializeField] private GameObject automaticSavingSlotPanel;
        [SerializeField] private GameObject latestPanel;
        [SerializeField] private Image factionImage;
        [SerializeField] private Image subFactionImage;

        public void SetIndex(int index)
        {
            _slotValue = index;
            automaticSavingSlotPanel.SetActive(_slotValue == SaveUtilities.AutomaticSaveSlot);
            _path = SaveUtilities.GetSaveSlotFolder(_slotValue);
        }

        public void SetLatestBorderEnable(bool enable)
        {
            latestPanel.SetActive(enable);
        }
        public DateTime GetLastSaveTime()
        {
            return !SaveUtilities.TryGetQuickData(_slotValue, out _)
                ? DateTime.MinValue
                : SaveUtilities.GetQuickDataLastWriteTime(_slotValue);
        }

        public void TrySaveSlot()
        {
            var hasPath = Directory.Exists(_path);
            if (_slotValue == SaveUtilities.AutomaticSaveSlot)
            {
                ConfirmWindow.Instance.Show("Auto save slot cannot be overwritten",showCancelButton: false);
                return;
            }
            if (hasPath)
            {
                ConfirmWindow.Instance.Show("The slot already exists, overwrite?",
                    () =>
                    {
                        CustomCoroutineRunner.Instance.StartCoroutine(SaveLoadController.Instance.SaveAsync(SaveType.Manual, _slotValue));
                        SaveLoadMenu.Instance.Hide();
                        GameController.Instance.ResumeGame(false);
                    });
            }
            else
            {
                SaveUtilities.InitializeSaveSlotFolder(_path, _slotValue);
                CustomCoroutineRunner.Instance.StartCoroutine(SaveLoadController.Instance.SaveAsync(SaveType.Manual, _slotValue));
                SaveLoadMenu.Instance.Hide();
                GameController.Instance.ResumeGame(false);
            }
        }

        public void TryLoadSlot(bool ifFirstTimeStartGame)
        {
            if (!SaveUtilities.TryGetQuickData(_slotValue, out var quickData))
            {
                ConfirmWindow.Instance.Show("Saving slot does not exist", showCancelButton: false);
            }
            else
            {
                if (ifFirstTimeStartGame)
                {
                    CustomCoroutineRunner.Instance.StartCoroutine(GameController.Instance.StartGameFirstTime(quickData.Faction, false, _slotValue,
                        quickData.CityPrefabId));
                    MenuOutController.Instance.HideMainMenu();
                }
                else CustomCoroutineRunner.Instance.StartCoroutine(GameController.Instance.LoadSavingSlot(_slotValue, quickData));
                SaveLoadMenu.Instance.Hide();
            }
        }


        private void Awake()
        {
            latestPanel.SetActive(false);
        }

        private void OnEnable()
        {
            var hasDirectory = Directory.Exists(_path);
            string info;
            if (hasDirectory)
            {
                if (SaveUtilities.TryGetQuickData(_slotValue, out var quickData))
                {
                    var locationName = "Main World";
                    if (quickData.CityPrefabId != 0)
                    {
                        locationName = DatabaseManager.CityDatabaseSo.GetItemById(quickData.CityPrefabId).gameplayName;
                    }

                    info =
                        $" {quickData.TotalHours} hours : {locationName} ";
                    factionImage.enabled = true;
                    subFactionImage.enabled = true;
                    factionImage.sprite = BasicUIResourceManager.Instance.GeneralFactionIconSprites[quickData.Faction];
                    subFactionImage.sprite =
                        BasicUIResourceManager.Instance.SubFactionIconSprites[quickData.SubFaction];
                }
                else
                {
                    info = "Lose saving";
                    factionImage.enabled = false;
                    subFactionImage.enabled = false;
                }
            }
            else
            {
                info = "Empty slot";
                factionImage.enabled = false;
                subFactionImage.enabled = false;
            }

            deleteButton.SetActive(hasDirectory && _slotValue != SaveUtilities.AutomaticSaveSlot);
            content.text = info;
        }

        private void Start()
        {
            deleteButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                var path = SaveUtilities.GetSaveSlotFolder(_slotValue);
                Directory.Delete(path, true);
                deleteButton.SetActive(false);
                content.text = "Empty slot";
            });
        }

        private string _path;
        private int _slotValue;
    }
}
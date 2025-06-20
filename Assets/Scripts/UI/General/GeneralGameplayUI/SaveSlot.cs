using System.IO;
using SparFlame.Components.General;
using SparFlame.Core.Utils;
using SparFlame.Systems.General.BasicControl;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;

namespace SparFlame.UI.General
{
    public class SaveSlot : MonoBehaviour
    {
        [SerializeField] private TMP_Text content;
        [SerializeField] private int slotValue;
        [SerializeField] private GameObject deleteButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private bool isLightFaction;
        private void Start()
        {
            var path = FolderPathUtils.GetPlayerSaveSlotFolder(slotValue);
            content.text = !Directory.Exists(path) ? "New Saving" : $"Loading saving {slotValue}";
            deleteButton.SetActive(Directory.Exists(path));
            deleteButton.GetComponent<Button>().onClick.AddListener(() =>
            {
                 Directory.Delete(path, true);
                 deleteButton.SetActive(false);
                 content.text = "New Saving";
            });
            saveButton.onClick.AddListener(() =>
            {
                GameController.Instance.PlayerChooseSavingSlot(slotValue);
                GameController.Instance.PlayerChooseFaction(isLightFaction ? FactionTag.Ally : FactionTag.Enemy);
            });
        }
    }
}
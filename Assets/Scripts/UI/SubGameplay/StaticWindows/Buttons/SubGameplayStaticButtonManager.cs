using SparFlame.Components.General;
using SparFlame.Systems.General.BasicControl;
using SparFlame.Systems.General.Battle;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.SubGameplay.StaticWindows.Buttons
{
    public class SubGameplayStaticButtonManager : MonoBehaviour
    {
        [SerializeField] private GameObject constructButton;
        [SerializeField] private GameObject armyGroupManageButton;
        [SerializeField] private GameObject enterAccurateSelectionButton;
        [SerializeField] private GameObject backToMainWorldButton;
        [SerializeField] private GameObject retreatButton;

        public static SubGameplayStaticButtonManager Instance;

        #region ButtonMethod

        public void OnClickBackToMainWorldButton()
        {
            if (_isSaving)
            {
                ConfirmWindow.Instance.Show("You cannot go back to main world while saving not complete",
                    showCancelButton: false);
                return;
            }
            StartCoroutine(GameController.Instance.SubWorldToMainWorld());
        }

        public void OnClickRetreatButton()
        {
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            using var query = em.CreateEntityQuery(typeof(PlayerRetreatRequest));
            using var query2 = em.CreateEntityQuery(typeof(BattleEndRequest));
            if (!query.IsEmpty || !query2.IsEmpty) return;
            BattleUtils.StartRetreat(em, true);
        }

        #endregion

        public void TogglePlayerCityUI(bool enable)
        {
            backToMainWorldButton.SetActive(enable);
            constructButton.SetActive(enable);
            armyGroupManageButton.SetActive(enable);
            enterAccurateSelectionButton.SetActive(enable);
        }

        public void ToggleBattleUI(bool enable)
        {
            retreatButton.SetActive(enable);
        }

        private bool _isSaving;

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }


        private void Start()
        {
            constructButton.SetActive(false);
            armyGroupManageButton.SetActive(false);
            enterAccurateSelectionButton.SetActive(false);
            backToMainWorldButton.SetActive(false);
            backToMainWorldButton.GetComponent<Button>().onClick.AddListener(OnClickBackToMainWorldButton);
            retreatButton.GetComponent<Button>().onClick.AddListener(OnClickRetreatButton);

            SaveLoadController.Instance.OnStartSave += _ => { _isSaving = true; };
            SaveLoadController.Instance.OnSaveComplete += _ => { _isSaving = false; };
        }
    }
}
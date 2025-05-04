using SparFlame.BootStrapper;
using SparFlame.GamePlaySystem.Command;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.CustomInput;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace SparFlame.UI.GamePlay
{
    /// <summary>
    /// This controller control all sub controllers about unit info, including :
    /// Unit2DShow, UnitInteractAbilityShow, UnitDetailShow, 
    /// </summary>
    public class InfoWindowController : MonoBehaviour
    {
        // public static InfoWindowController Instance;


        [Header("Custom config")] public GameObject infoPanel;
        [SerializeField] private GameObject maximizeButton;

        public static InfoWindowController Instance;

        public void OnMinimizeClick()
        {
            _minimizeWindow = !_minimizeWindow;
            Hide();
        }

        public void OnMaximizeClick()
        {
            _minimizeWindow = !_minimizeWindow;
            maximizeButton.SetActive(false);
            Show();
        }

        /// <summary>
        /// Will close all incorrect opened detail windows for target entity
        /// But WILL NOT OPEN correct detail window, you have to open it manually before calling this methods
        /// </summary>
        /// <param name="target"></param>
        public void UpdateCloseUpTarget(Entity target)
        {
            _closeUpTarget = target;
            if (CloseUpWindow.Instance.TrySwitchTarget(_closeUpTarget))
            {
                if (!CloseUpWindow.Instance.IsOpened()) CloseUpWindow.Instance.Show();
            }

            if (UnitDetailWindow.Instance.IsOpened())
            {
                if (!UnitDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    UnitDetailWindow.Instance.Hide();
            }

            if (InteractAbilityWindow.Instance.IsOpened())
            {
                if (!InteractAbilityWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    InteractAbilityWindow.Instance.Hide();
            }

            if (BuildingDetailWindow.Instance.IsOpened())
            {
                if (!BuildingDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    BuildingDetailWindow.Instance.Hide();
            }

            if (ResourceDetailWindow.Instance.IsOpened())
            {
                if (!ResourceDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    ResourceDetailWindow.Instance.Hide();
            }
        }

        private bool _minimizeWindow;
        private CustomInputActions _customInputActions;

        private Entity _closeUpTarget;
        private EntityManager _em;
        private EntityQuery _gamingTag;
        private EntityQuery _customMouseDataQuery;
        private EntityQuery _cursorData;
        private EntityQuery _selectedData;


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
            _gamingTag = _em.CreateEntityQuery(typeof(GamingTag));
            _customMouseDataQuery = _em.CreateEntityQuery(typeof(InputMouseData));
            _cursorData = _em.CreateEntityQuery(typeof(CursorData));
            _selectedData = _em.CreateEntityQuery(typeof(UnitSelectionData));
            _customInputActions = InputListener.Instance.GetCustomInputActions();
            infoPanel.SetActive(false);
        }

        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!_em.Exists(_closeUpTarget))
                _closeUpTarget = Entity.Null;

            var inputMouseData = _customMouseDataQuery.GetSingleton<InputMouseData>();
            var cursorData = _cursorData.GetSingleton<CursorData>();
            var selectedData = _selectedData.GetSingleton<UnitSelectionData>();

            // Check left click event
            // Valid when left click on interactable entity
            var leftClickOnValid = !inputMouseData.IsOverUI
                                   && _customInputActions.InfoWindow.CheckInfo.WasPerformedThisFrame()
                                   && cursorData.LeftCursorType is not (CursorType.None);
            var leftClickOnInvalid = !inputMouseData.IsOverUI
                                     && _customInputActions.InfoWindow.CheckInfo.WasPerformedThisFrame()
                                     && cursorData.LeftCursorType is (CursorType.None);

            // Check should switch close up target
            if (leftClickOnValid)
            {
                UpdateCloseUpTarget(inputMouseData.HitEntity);
            }


            // Check should show or hide info window
            // show info window when select some units or left click on valid
            var shouldShowInfoWindow = selectedData.CurrentSelectCount > 0 || leftClickOnValid;


            if (shouldShowInfoWindow)
            {
                if (!_minimizeWindow && !infoPanel.activeSelf)
                    Show();
                else if (_minimizeWindow)
                    maximizeButton.SetActive(true);
            }


            // Check should show or hide Unit multi 2D , interact , detail window
            // Show multi unit window when select count > 1
            // Show interact and detail when select count <= 1 and closeUpTarget not null
            var shouldShowUnitMulti2D = infoPanel.activeSelf && selectedData.CurrentSelectCount > 1;
            var shouldShowInteractAndDetail = infoPanel.activeSelf && selectedData.CurrentSelectCount <= 1 &&
                                              _closeUpTarget != Entity.Null;
            if (shouldShowUnitMulti2D)
            {
                if (!UnitMulti2DWindow.Instance.IsOpened())
                {
                    UnitMulti2DWindow.Instance.Show();
                    UnitMulti2DWindow.Instance
                        .OnClickSlot(
                            0); // TODO : Due to the unit multi system update after the multi window is opened, this may not show when first drag select
                }
            }
            else
            {
                if (UnitMulti2DWindow.Instance.IsOpened())
                    UnitMulti2DWindow.Instance.Hide();
            }

            if (shouldShowInteractAndDetail)
            {
                if (!InteractAbilityWindow.Instance.IsOpened() &&
                    InteractAbilityWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    InteractAbilityWindow.Instance.Show();
                if (!UnitDetailWindow.Instance.IsOpened() &&
                    UnitDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    UnitDetailWindow.Instance.Show();
                if (!BuildingDetailWindow.Instance.IsOpened() &&
                    BuildingDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    BuildingDetailWindow.Instance.Show();
                if (!ResourceDetailWindow.Instance.IsOpened() &&
                    ResourceDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    ResourceDetailWindow.Instance.Show();
            }
            else
            {
                if (InteractAbilityWindow.Instance.IsOpened())
                    InteractAbilityWindow.Instance.Hide();
                if (UnitDetailWindow.Instance.IsOpened())
                    UnitDetailWindow.Instance.Hide();
                if (BuildingDetailWindow.Instance.IsOpened())
                    BuildingDetailWindow.Instance.Hide();
                if (ResourceDetailWindow.Instance.IsOpened())
                    ResourceDetailWindow.Instance.Hide();
            }

            var shouldHideInfoWindow = leftClickOnInvalid
                                       || _customInputActions.InfoWindow.CloseWindow.WasPerformedThisFrame()
                                       || (!UnitDetailWindow.Instance.HasTarget()
                                           && !BuildingDetailWindow.Instance.HasTarget()
                                           && !InteractAbilityWindow.Instance.HasTarget()
                                           && !UnitMulti2DWindow.Instance.HasTarget()
                                           && !ResourceDetailWindow.Instance.HasTarget()
                                           && !CloseUpWindow.Instance.HasTarget());
            if (shouldHideInfoWindow)
            {
                if (_minimizeWindow)
                    maximizeButton.SetActive(false);
                else if (!_minimizeWindow && infoPanel.activeSelf)
                    Hide();
            }
        }

        public void Show()
        {
            infoPanel.SetActive(true);
            // Close up window should always open with unit info window and should never be blank
            // Only when unit info closed , it is allowed to be blank, but it will close at the same time
            CloseUpWindow.Instance.Show();
        }

        public void Hide()
        {
            infoPanel.SetActive(false);
            CloseUpWindow.Instance.Hide();
        }


        private void ScrollUpWindow()
        {
        }

        private void ScrollDownWindow()
        {
        }
    }
}
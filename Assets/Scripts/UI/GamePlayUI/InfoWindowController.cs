using SparFlame.GamePlaySystem.Command;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace SparFlame.UI.GamePlay
{
    /// <summary>
    /// This controller control all sub controllers about unit info, including :
    /// Unit2DShow, UnitInteractAbilityShow, UnitDetailShow, 
    /// </summary>
    public class InfoWindowController : MonoBehaviour
    {
        public static InfoWindowController Instance;

        
        [Header("Custom config")] public GameObject infoPanel;
        public GameObject maxmizeButton;
        public void OnMinimizeClick()
        {
            _minimizeWindow = !_minimizeWindow;
            Hide();
        }

        public void OnMaximizeClick()
        {
            _minimizeWindow = !_minimizeWindow;
            maxmizeButton.SetActive(false);
            Show();
        }
        
        public void UpdateCloseUpTarget(Entity target)
        {
            _closeUpTarget = target;
            CloseUpWindow.Instance.TrySwitchTarget(_closeUpTarget);
            UnitDetailWindow.Instance.TrySwitchTarget(_closeUpTarget);
            InteractAbilityWindow.Instance.TrySwitchTarget(_closeUpTarget);
        }

        private bool _minimizeWindow;

        private Entity _closeUpTarget;
        private EntityManager _em;
        private EntityQuery _notPauseTag;
        private EntityQuery _customInputData;
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
            _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
            _customInputData = _em.CreateEntityQuery(typeof(CustomInputSystemData));
            _cursorData = _em.CreateEntityQuery(typeof(CursorData));
            _selectedData = _em.CreateEntityQuery(typeof(UnitSelectionData));
            Hide();
        }

        private void Update()
        {
            if (_notPauseTag.IsEmpty) return;

            var customInputSystemData = _customInputData.GetSingleton<CustomInputSystemData>();
            var cursorData = _cursorData.GetSingleton<CursorData>();
            var selectedData = _selectedData.GetSingleton<UnitSelectionData>();

            // Check left click event
            // Valid when left click on interactable entity
            var leftClickOnValid = customInputSystemData is
                                       { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left, IsOverUI: false }
                                   && cursorData.LeftCursorType is not (CursorType.None);
            var leftClickOnInvalid = customInputSystemData is
                                         { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left, IsOverUI: false }
                                     && cursorData.LeftCursorType is (CursorType.None);

            // Check should switch close up target
            if (leftClickOnValid)
            {
                UpdateCloseUpTarget(customInputSystemData.HitEntity);
            }


            // Check should show or hide info window
            // show info window when select some units or left click on valid
            var shouldShowInfoWindow =  selectedData.CurrentSelectCount > 0 || leftClickOnValid;
            
            var shouldHideInfoWindow =   leftClickOnInvalid;

            if (shouldShowInfoWindow )
            {
                if(!_minimizeWindow && !infoPanel.activeSelf)
                    Show();
                else if (_minimizeWindow)
                    maxmizeButton.SetActive(true);
            }

            if (shouldHideInfoWindow )
            {
                if(_minimizeWindow)
                    maxmizeButton.SetActive(false);
                else if(!_minimizeWindow && infoPanel.activeSelf)
                    Hide();
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
                    UnitMulti2DWindow.Instance.Show();
                // Unit multi 2D will default show slot 0 as close up target if there is none
                if (UnitMulti2DWindow.Instance.IsOpened() && !CloseUpWindow.Instance.HasTarget())
                    UnitMulti2DWindow.Instance.OnClickUnit2D(0);
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
            }
            else
            {
                if (InteractAbilityWindow.Instance.IsOpened())
                    InteractAbilityWindow.Instance.Hide();
                if (UnitDetailWindow.Instance.IsOpened())
                    UnitDetailWindow.Instance.Hide();
            }

        }

        private void Show()
        {
            infoPanel.SetActive(true);
            // Close up window should always open with unit info window and should never be blank
            // Only when unit info closed , it is allowed to be blank, but it will close at the same time
            CloseUpWindow.Instance.Show();
        }

        private void Hide()
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
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.SubGameplay;
using SparFlame.Systems.General.Input;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.SubGameplay
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
            ClearCloseUpTarget();
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
            if (SubGameplayCloseUpWindow.Instance.TrySwitchTarget(_closeUpTarget))
            {
                if (!SubGameplayCloseUpWindow.Instance.IsOpened()) SubGameplayCloseUpWindow.Instance.Show();
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
        private bool _ifLastTimePlayerCloseByEsc;

        private Entity _closeUpTarget;
        private EntityManager _em;
        private EntityQuery _gamingTag;
        private EntityQuery _customMouseDataQuery;
        private EntityQuery _cursorData;
        private EntityQuery _selectedData;


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
            _gamingTag = _em.CreateEntityQuery(typeof(SubGamingTag));
            _customMouseDataQuery = _em.CreateEntityQuery(typeof(InputMouseData));
            _cursorData = _em.CreateEntityQuery(typeof(SubGameplayCursorData));
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
            var cursorData = _cursorData.GetSingleton<SubGameplayCursorData>();
            var selectedData = _selectedData.GetSingleton<UnitSelectionData>();
            
            
              
                
            // Check left click event
            // Valid when left click on interactable entity
            var leftClickOnValid = !inputMouseData.IsOverUI
                                   && _customInputActions.InfoWindow.CheckInfo.WasPerformedThisFrame()
                                   && cursorData.LeftCursorType is not (SubGameplayCursorType.None);
            var leftClickOnInvalid = !inputMouseData.IsOverUI
                                     && _customInputActions.InfoWindow.CheckInfo.WasPerformedThisFrame()
                                     && cursorData.LeftCursorType is (SubGameplayCursorType.None);

            // Check should switch close up target
            if (leftClickOnValid)
            {
                UpdateCloseUpTarget(inputMouseData.HitEntity);
            }
            

            // Check should show or hide info window
            // show info window when select some units or left click on valid
            var shouldShowInfoWindow = selectedData.CurrentSelectCount > 0 || leftClickOnValid;
            if (selectedData.DragSelectStart)
                _ifLastTimePlayerCloseByEsc = false;

            if (shouldShowInfoWindow)
            {
                if(leftClickOnValid || !_ifLastTimePlayerCloseByEsc )
                {
                    _ifLastTimePlayerCloseByEsc = false;
                    if (!_minimizeWindow && !infoPanel.activeSelf)
                        Show();
                    else if (_minimizeWindow)
                        maximizeButton.SetActive(true);
                    if (!SubGameplayCloseUpWindow.Instance.HasTarget())
                    {
                        UnitMulti2DWindow.Instance.OnClickSlot(0);
                    }
                }
               
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
                    UnitMulti2DWindow.Instance.DisableClickRoutine();
                    UnitMulti2DWindow.Instance
                        .OnClickSlot(
                            0);
                    UnitDetailWindow.Instance.Hide();
                    InteractAbilityWindow.Instance.Hide();
                    UnitMulti2DWindow.Instance.EnableClickRoutine();
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
                if (selectedData.CurrentSelectCount<=1)
                {
                    if (InteractAbilityWindow.Instance.IsOpened())
                        InteractAbilityWindow.Instance.Hide();
                    if (UnitDetailWindow.Instance.IsOpened())
                        UnitDetailWindow.Instance.Hide();
                }
                
                if (BuildingDetailWindow.Instance.IsOpened())
                    BuildingDetailWindow.Instance.Hide();
                if (ResourceDetailWindow.Instance.IsOpened())
                    ResourceDetailWindow.Instance.Hide();
            }

            var closeByEsc = _customInputActions.InfoWindow.CloseWindow.WasPerformedThisFrame();
            var shouldHideInfoWindow = leftClickOnInvalid
                                       || closeByEsc
                                       || (!UnitDetailWindow.Instance.HasTarget()
                                           && !BuildingDetailWindow.Instance.HasTarget()
                                           && !InteractAbilityWindow.Instance.HasTarget()
                                           && !UnitMulti2DWindow.Instance.HasTarget()
                                           && !ResourceDetailWindow.Instance.HasTarget()
                                           && !SubGameplayCloseUpWindow.Instance.HasTarget());
            if (shouldHideInfoWindow)
            {
                if (closeByEsc && UnitMulti2DWindow.Instance.IsOpened() && UnitDetailWindow.Instance.IsOpened())
                {
                    UnitDetailWindow.Instance.Hide();
                    InteractAbilityWindow.Instance.Hide();
                }
                else
                {
                    if (_minimizeWindow)
                        maximizeButton.SetActive(false);
                    else if (!_minimizeWindow && infoPanel.activeSelf)
                        Hide();
                    if(closeByEsc) _ifLastTimePlayerCloseByEsc = true;
                    ClearCloseUpTarget();
                }
            }
        }

        public void Show()
        {
            infoPanel.SetActive(true);
            // Close up window should always open with unit info window and should never be blank
            // Only when unit info closed , it is allowed to be blank, but it will close at the same time
            SubGameplayCloseUpWindow.Instance.Show();
        }

        public void Hide()
        {
            infoPanel.SetActive(false);
            SubGameplayCloseUpWindow.Instance.Hide();
        }

        private void ClearCloseUpTarget()
        {
            SubGameplayCloseUpWindow.Instance.ClearCloseUpTarget();
            UnitDetailWindow.Instance.ClearCloseUpTarget();
            BuildingDetailWindow.Instance.ClearCloseUpTarget();
            ResourceDetailWindow.Instance.ClearCloseUpTarget();
            InteractAbilityWindow.Instance.ClearCloseUpTarget();
            SubGameplayGarrisonInfoWindow.Instance.ClearCloseUpTarget();
            ConjureQueueWindow.Instance.ClearCloseUpTarget();
            MiniConjureWindow.Instance.ClearCloseUpTarget();
            ConjureWindow.Instance.ClearCloseUpTarget();
        }


        private void ScrollUpWindow()
        {
        }

        private void ScrollDownWindow()
        {
        }
    }
}
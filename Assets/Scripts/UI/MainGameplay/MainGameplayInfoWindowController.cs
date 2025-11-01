using System;
using SparFlame.Components.General;
using SparFlame.Components.Input;
using SparFlame.Components.MainGameplay;
using SparFlame.UI.General;
using Unity.Entities;
using UnityEngine;

namespace SparFlame.UI.MainGameplay
{
    /// <summary>
    /// This controller control all sub controllers about unit info, including :
    /// Unit2DShow, UnitInteractAbilityShow, UnitDetailShow, 
    /// </summary>
    public class MainGameplayInfoWindowController : MonoBehaviour
    {
        // public static InfoWindowController Instance;


        [Header("Custom config")] public GameObject infoPanel;
        [SerializeField] private GameObject maximizeButton;

        public static MainGameplayInfoWindowController Instance;

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
            if (ArmyGroupDetailWindow.Instance.IsOpened())
            {
                if (!ArmyGroupDetailWindow.Instance.TrySwitchTarget(target)) ArmyGroupDetailWindow.Instance.Hide();
            }

            if (CityDetailWindow.Instance.IsOpened())
            {
                if (!CityDetailWindow.Instance.TrySwitchTarget(target)) CityDetailWindow.Instance.Hide();
            }
        }

        private bool _minimizeWindow;
        private bool _ifLastTimePlayerCloseByEsc;

        private Entity _closeUpTarget;
        private EntityManager _em;
        private EntityQuery _gamingTag;
        private EntityQuery _customMouseDataQuery;
        private EntityQuery _cursorData;
        private EntityQuery _selectedData;
        private EntityQuery _generalShortcutData;

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
            _gamingTag = _em.CreateEntityQuery(typeof(MainGamingTag));
            _customMouseDataQuery = _em.CreateEntityQuery(typeof(InputMouseData));
            _cursorData = _em.CreateEntityQuery(typeof(MainGameplayCursorData));
            _selectedData = _em.CreateEntityQuery(typeof(ArmyGroupSelectionData));
            _generalShortcutData = _em.CreateEntityQuery(typeof(InputGeneralShortcutData));
            infoPanel.SetActive(false);
        }

        private void Update()
        {
            if (_gamingTag.IsEmpty) return;
            if (!_em.Exists(_closeUpTarget))
                _closeUpTarget = Entity.Null;

            var inputMouseData = _customMouseDataQuery.GetSingleton<InputMouseData>();
            var cursorData = _cursorData.GetSingleton<MainGameplayCursorData>();
            var selectedData = _selectedData.GetSingleton<ArmyGroupSelectionData>();
            var generalShortcutData = _generalShortcutData.GetSingleton<InputGeneralShortcutData>();
            // Check left click event
            // Valid when left click on interactable entity
            var checkInfoPerformed = generalShortcutData.CheckInfo;
            var leftClickOnValid = !inputMouseData.IsOverUI
                                   && checkInfoPerformed
                                   && cursorData.CursorType != MainGameplayCursorType.None
                                   && cursorData.CursorType != MainGameplayCursorType.March;

            var leftClickOnInvalid = !inputMouseData.IsOverUI
                                     && checkInfoPerformed
                                     && cursorData.CursorType is MainGameplayCursorType.None
                                         or MainGameplayCursorType.March;
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
                if (leftClickOnValid || !_ifLastTimePlayerCloseByEsc)
                {
                    _ifLastTimePlayerCloseByEsc = false;
                    if (!_minimizeWindow && !infoPanel.activeSelf)
                        Show();
                    else if (_minimizeWindow)
                        maximizeButton.SetActive(true);
                    if (_closeUpTarget == Entity.Null)
                    {
                        ArmyGroupMulti2DWindow.Instance.OnClickSlot(0);
                    }
                }
            }

            // Check should show or hide Unit multi 2D , interact , detail window
            // Show multi unit window when select count > 1
            // Show interact and detail when select count <= 1 and closeUpTarget not null
            var shouldShowMulti2D = infoPanel.activeSelf && selectedData.CurrentSelectCount > 1;
            var shouldShowInteractAndDetail = infoPanel.activeSelf && selectedData.CurrentSelectCount <= 1 &&
                                              _closeUpTarget != Entity.Null;
            if (shouldShowMulti2D)
            {
                if (!ArmyGroupMulti2DWindow.Instance.IsOpened())
                {
                    ArmyGroupMulti2DWindow.Instance.Show();
                    ArmyGroupMulti2DWindow.Instance.DisableClickRoutine();
                    ArmyGroupMulti2DWindow.Instance
                        .OnClickSlot(
                            0);
                    ArmyGroupDetailWindow.Instance.Hide();
                    ArmyGroupMulti2DWindow.Instance.EnableClickRoutine();
                }
            }
            else
            {
                if (ArmyGroupMulti2DWindow.Instance.IsOpened())
                    ArmyGroupMulti2DWindow.Instance.Hide();
            }

            if (shouldShowInteractAndDetail)
            {
                if (!ArmyGroupDetailWindow.Instance.IsOpened() &&
                    ArmyGroupDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    ArmyGroupDetailWindow.Instance.Show();
                if (!CityDetailWindow.Instance.IsOpened() &&
                    CityDetailWindow.Instance.TrySwitchTarget(_closeUpTarget))
                    CityDetailWindow.Instance.Show();
            }
            else
            {
                if (selectedData.CurrentSelectCount <= 1)
                {
                    if (ArmyGroupDetailWindow.Instance.IsOpened())
                        ArmyGroupDetailWindow.Instance.Hide();
                }

                if (CityDetailWindow.Instance.IsOpened())
                    CityDetailWindow.Instance.Hide();
            }

            var closeByEsc = generalShortcutData.CloseWindow;
            var shouldHideInfoWindow = leftClickOnInvalid
                                       || closeByEsc
                                       || (!ArmyGroupDetailWindow.Instance.HasTarget()
                                           && !CityDetailWindow.Instance.HasTarget()
                                           && !ArmyGroupMulti2DWindow.Instance.HasTarget()
                                           /*&& !MainGameplayCloseUpWindow.Instance.HasTarget()*/);
            if (shouldHideInfoWindow)
            {
                if (closeByEsc && ArmyGroupMulti2DWindow.Instance.IsOpened() &&
                    ArmyGroupDetailWindow.Instance.IsOpened())
                {
                    ArmyGroupDetailWindow.Instance.Hide();
                }
                else
                {
                    if (_minimizeWindow)
                        maximizeButton.SetActive(false);
                    else if (!_minimizeWindow && infoPanel.activeSelf)
                        Hide();
                    if (closeByEsc) _ifLastTimePlayerCloseByEsc = true;
                    ClearCloseUpTarget();
                }
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_gamingTag != default)
                    _gamingTag.Dispose();
                if (_customMouseDataQuery != default)
                    _customMouseDataQuery.Dispose();
                if (_cursorData != default)
                    _cursorData.Dispose();
                if (_selectedData != default)
                    _selectedData.Dispose();
                if (_generalShortcutData != default)
                    _generalShortcutData.Dispose();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void Show()
        {
            infoPanel.SetActive(true);
            // Close up window should always open with unit info window and should never be blank
            // Only when unit info closed , it is allowed to be blank, but it will close at the same time
            // MainGameplayCloseUpWindow.Instance.Show();
        }

        public void Hide()
        {
            ClearCloseUpTarget();
            infoPanel.SetActive(false);
            _closeUpTarget = Entity.Null;
            // MainGameplayCloseUpWindow.Instance.Hide();
        }

        private void ClearCloseUpTarget()
        {
            _closeUpTarget = Entity.Null;
            // MainGameplayCloseUpWindow.Instance.ClearCloseUpTarget();
            ArmyGroupDetailWindow.Instance.ClearCloseUpTarget();
            CityDetailWindow.Instance.ClearCloseUpTarget();

            // ConjureQueueWindow.Instance.ClearCloseUpTarget();
            // MiniConjureWindow.Instance.ClearCloseUpTarget();
            // ConjureWindow.Instance.ClearCloseUpTarget();
        }
    }
}
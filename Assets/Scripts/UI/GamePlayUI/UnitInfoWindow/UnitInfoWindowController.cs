using System;
using SparFlame.GamePlaySystem.Command;
using SparFlame.GamePlaySystem.General;
using SparFlame.GamePlaySystem.Interact;
using SparFlame.GamePlaySystem.Mouse;
using SparFlame.GamePlaySystem.Units;
using SparFlame.GamePlaySystem.UnitSelection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

public class UnitInfoWindowController : MonoBehaviour
{
    public GameObject UnitInfoWindow;
    public bool isEnabled;
    private EntityManager _em;
    private EntityQuery _notPauseTag;
    private EntityQuery _customInputData;
    private EntityQuery _cursorData;
    private EntityQuery _selectedData;
    private bool _minimizeWindow = false;
    private Entity _closeUpTarget;

    void Start()
    {
        _em = World.DefaultGameObjectInjectionWorld.EntityManager;
        _notPauseTag = _em.CreateEntityQuery(typeof(NotPauseTag));
        _customInputData = _em.CreateEntityQuery(typeof(CustomInputSystemData));
        _cursorData = _em.CreateEntityQuery(typeof(CursorData));
        _selectedData = _em.CreateEntityQuery(typeof(UnitSelectionData));
        UnitInfoWindow.SetActive(false);
        isEnabled = false;
    }

    void Update()
    {
        if (_notPauseTag.IsEmpty) return;
        var data = _customInputData.GetSingleton<CustomInputSystemData>();
        var cursorData = _cursorData.GetSingleton<CursorData>();
        var selectedData = _selectedData.GetSingleton<UnitSelectionData>();
        
        var shouldShowUnitInfoWindow = false;
        var shouldHideUnitInfoWindow = false;
        var selected = selectedData.CurrentSelectCount > 0;
        // When cursor is none or ui or gather, left click will not show window. Gather will not affect window show or hide
        var leftClickOnValid = data is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left }
                               && cursorData.LeftCursorType is not (CursorType.None or CursorType.UI or CursorType.Gather);
        var leftClickOnInvalid = data is { ClickFlag: ClickFlag.Start, ClickType: ClickType.Left }
                                 && cursorData.LeftCursorType is (CursorType.None or CursorType.UI);
        
        // When window closed and select some units or left click on valid ,should show window
        if (!isEnabled && !_minimizeWindow && (selected || leftClickOnValid)) shouldShowUnitInfoWindow = true;
        // When window open and select none and current close up target is selected, should hide window
        if (isEnabled && !selected && _em.HasComponent<Selected>(_closeUpTarget)) shouldHideUnitInfoWindow = true;
        // When window open and current close up target is not selectable, and click on somewhere else, should hide window
        if(isEnabled && !_em.HasComponent<Selected>(_closeUpTarget) && leftClickOnInvalid)shouldHideUnitInfoWindow = true;
        
        
        if (shouldShowUnitInfoWindow)
        {
            ShowUnitInfoWindow();
            ScrollUpWindow();
        }

        if (shouldHideUnitInfoWindow)
        {
            HideUnitInfoWindow();
            ScrollDownWindow();
        }

        if (leftClickOnValid)
        {
            _closeUpTarget = data.HitEntity;
        }

        var interactableAttr = _em.GetComponentData<InteractableAttr>(data.HitEntity);
        var statData = _em.GetComponentData<StatData>(data.HitEntity);
        switch (interactableAttr.BaseTag)
        {
            case BaseTag.Units:
                var unitAttr = _em.GetComponentData<UnitBasicAttr>(data.HitEntity);
                break;
            case BaseTag.Buildings:
                break;
            case BaseTag.Resources:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void HideUnitInfoWindow()
    {
        UnitInfoWindow.SetActive(false);
        isEnabled = false;
    }

    private void ShowUnitInfoWindow()
    {
        UnitInfoWindow.SetActive(true);
        isEnabled = true;
    }

    private void ScrollUpWindow()
    {
    }

    private void ScrollDownWindow()
    {
    }
}
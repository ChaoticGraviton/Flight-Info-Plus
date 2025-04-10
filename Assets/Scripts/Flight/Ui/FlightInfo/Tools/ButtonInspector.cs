using Assets.Scripts;
using ModApi.Flight.Events;
using ModApi.Ui.Inspector;
using System.Collections.Generic;
using UnityEngine;
using ModApi.Flight;
using System;
using Assets.Scripts.Flight;

public class ButtonInspector
{
    internal IInspectorPanel _groupModelInspector;
    internal InspectorModel _inspectorModel;
    private Vector2? _currentOffset;
    private InspectorPanelCreationInfo creationInfo;
    internal List<ItemModel> _ItemModels;
    internal string _modelName;
    internal bool _visible;
    internal Vector2? VisibilityState => Game.Instance.Settings.UserPrefs.GetVector2OrNull(_inspectorModel.UserPrefsId + ".Visible");
    internal Vector2 openState = new(1, 0);
    internal Vector2 closedState = new(0, 0);
    internal Vector2 mapOpened = new(1, 1);
    internal bool _shouldSaveState = true;

    internal void Initialize()
    {
        _inspectorModel = new InspectorModel(_modelName + "-ID", _modelName);
        RefreshItems();
        Game.Instance.FlightScene.FlightEnded += FlightSceneEnd;
        Game.Instance.FlightScene.Initialized += FlightSceneInitalized;
        (Game.Instance.FlightScene.ViewManager as ViewManagerScript).ViewChanged += ViewChanged;
    }

    public bool InspectorVisible
    {
        get => _visible;
        set
        {
            if (value)
            {
                _visible = true;
                if (_groupModelInspector != null)
                {
                    _groupModelInspector.Visible = true;
                    Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, openState);
                }
                else
                    CreateInspector();
            }
            else
                ClosePanel();
        }
    }

    public void OnToggleButtonClicked(TextButtonModel model)
    {
        _shouldSaveState = true;
        InspectorVisible = !InspectorVisible;
    }

    private void CreateInspector()
    {
        InspetorCreationInfo();
        _groupModelInspector = Game.Instance.UserInterface.CreateInspectorPanel(_inspectorModel, creationInfo);
        _groupModelInspector.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
        _groupModelInspector.Pinned += ManagePin;
        _groupModelInspector.Unpinned += ManageMapUnpin;
        Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, openState);
    }

    private void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel)
    {
        _shouldSaveState = true;
        ClosePanel();
    }

    private void InspetorCreationInfo() => creationInfo = new()
    {
        StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
        PanelMaxHeight = 10,
        Resizable = true,
        StartOffset = _currentOffset ?? new Vector2(-100f, -90f)
    };

    private void ClosePanel()
    {
        if (_groupModelInspector == null) return;
        _currentOffset = new Vector2?(_groupModelInspector.Position);
        _visible = false;
        if (_shouldSaveState)
            Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, closedState);
        _groupModelInspector.Close();
        _groupModelInspector = null;
        _shouldSaveState = true;
    }

    private void FlightSceneInitalized(IFlightScene initializedObject) => MangeViewChangeVisbility(Game.Instance.FlightScene.ViewManager.GameView.RenderView);

    private void FlightSceneEnd(object sender, FlightEndedEventArgs e)
    {
        _visible = false;
        _groupModelInspector = null;
    }

    public void RefreshItems()
    {
        foreach (ItemModel item in _ItemModels)
            _inspectorModel.Add(item);
        _inspectorModel.Add(Mod.Instance.FlightInfoPlus.InspectorSpacerModel);
        if (_visible)
        {
            InspectorVisible = !InspectorVisible;
            InspectorVisible = !InspectorVisible;
        }
    }

    private void ViewChanged(object sender, EventArgs e) => MangeViewChangeVisbility(Game.Instance.FlightScene.ViewManager.GameView.RenderView);

    private void MangeViewChangeVisbility(bool gameView)
    {
        if (VisibilityState.HasValue)
        {
            if (_groupModelInspector != null && _groupModelInspector.IsPinned)
                Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, mapOpened);
            _shouldSaveState = false;
            bool inspectorVisbility = gameView ? VisibilityState.Value.x != 0 : VisibilityState.Value.y != 0;
            InspectorVisible = inspectorVisbility;
            return;
        }
    }

    private void ManagePin(IInspectorPanel panel) => Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, mapOpened);

    private void ManageMapUnpin(IInspectorPanel panel)
    {
        if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
        {
            Mod.Instance.UpdateInspectorPrefs(_groupModelInspector, openState);
            InspectorVisible = false;
        }
    }
}
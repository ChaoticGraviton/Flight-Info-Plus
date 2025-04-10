using Assets.Scripts;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.GameView.UI.Inspector;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.Flight.Ui.FlightInfo.FuelInfo;
using HarmonyLib;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Ui.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.Xml;
using UnityEngine;

public class FlightInfoPlus
{
    // Stock Flight Info
    public IList<GroupModel> FlightInfoGroups;
    internal IInspectorPanel FlightInfoPanel;
    public XmlElement FlightInfoButton;
    internal InspectorModel FlightInfoInspectorModel;

    // Main Flight Info Plus Inspector
    internal InspectorModel _inspectorModel;
    public IInspectorPanel _inspector;
    private Vector2? _currentOffset;
    private InspectorPanelCreationInfo creationInfo;
    public List<ButtonInspector> buttonInspectors;
    public bool _visible;
    internal bool _gameVisible;
    internal Vector2? VisibilityState => Game.Instance.Settings.UserPrefs.GetVector2OrNull(_inspectorModel.UserPrefsId + ".Visible");
    internal Vector2 openState = new(1, 0);
    internal Vector2 closedState = new(0, 0);
    internal Vector2 mapOpened = new(1, 1);
    internal bool _shouldSaveState = true;
    public SpacerModel InspectorSpacerModel = new();


    // Fuel Plus Inspector
    internal FuelInfo FuelInfo;

    // Craft Info
    internal CraftInfo craftInfo;

    internal void Initialize(InspectorModel flightInfoModel)
    {
        FlightInfoInspectorModel = flightInfoModel;
        FlightInfoGroups = flightInfoModel.Groups;
        FlightInfoPanel = flightInfoModel.Panel;

        // Creates the Flight Info+ inspector, and creates button inspector instances for each group model taking part of the stock Flight Info. 
        _inspectorModel = new InspectorModel("FlightInfoPlus", "Flight Info+");
        buttonInspectors = new List<ButtonInspector>();
        foreach (GroupModel group in FlightInfoGroups)
        {
            if (group.Name != null)
            {
                buttonInspectors.Add(new ButtonInspector());
                ButtonInspector currentInstance = buttonInspectors.Last();
                currentInstance._modelName = group.Name;
                if (group.Name == "Fuel")
                {
                    FuelInfo = new(currentInstance);
                    currentInstance._ItemModels = new();
                }
                else
                {
                    currentInstance._ItemModels = (List<ItemModel>)group.Items;
                }

                currentInstance.Initialize();
                _inspectorModel.Add(new TextButtonModel(group.Name, new Action<TextButtonModel>(currentInstance.OnToggleButtonClicked)));
            }
        }
        //craftInfo = new CraftInfo();
        //craftInfo.Initialize();
        //AddGroupModelButton(craftInfo.GroupModel);        
        _inspectorModel.Add(InspectorSpacerModel);

        Game.Instance.FlightScene.FlightEnded += FlightSceneEnd;
        Game.Instance.FlightScene.Initialized += FlightSceneInitalized;
        (Game.Instance.FlightScene.ViewManager as ViewManagerScript).ViewChanged += ViewChanged;
    }

    public bool FlightInfoVisible
    {
        get => _visible;
        set
        {
            if (value)
            {
                _visible = true;
                if (_inspector != null)
                {
                    _inspector.Visible = true;
                    Mod.Instance.UpdateInspectorPrefs(_inspector, openState);
                }
                else
                    CreateInspector();
            }
            else
                ClosePanel();
        }
    }

    public void OnFlightInfoButtonClicked()
    {
        _shouldSaveState = true;
        FlightInfoVisible = !FlightInfoVisible;
    }

    private void CreateInspector()
    {
        InspetorCreationInfo();
        _inspector = Game.Instance.UserInterface.CreateInspectorPanel(_inspectorModel, creationInfo);
        _inspector.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
        _inspector.Pinned += ManagePin;
        _inspector.Unpinned += ManageMapUnpin;
        Mod.Instance.UpdateInspectorPrefs(_inspector, openState);
    }

    private void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel)
    {
        _shouldSaveState = true;
        ClosePanel();
    }

    private void InspetorCreationInfo() => creationInfo = new()
    {
        StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
        PanelMaxHeight = 20,
        Resizable = true,
        StartOffset = _currentOffset ?? new Vector2(-170f, -90f)
    };

    private void ClosePanel()
    {
        if (_inspector == null) return;
        _currentOffset = new Vector2?(_inspector.Position);
        _visible = false;
        if (_shouldSaveState)
            Mod.Instance.UpdateInspectorPrefs(_inspector, closedState);
        _inspector.Close();
        _inspector = null;
        _shouldSaveState = true;
    }

    private void FlightSceneInitalized(IFlightScene initializedObject)
    {
        ManageViewVisibility(Game.Instance.FlightScene.ViewManager.GameView.RenderView);
    }

    private void FlightSceneEnd(object sender, FlightEndedEventArgs e)
    {
        _visible = false;
        _inspector = null;
        Game.Instance.FlightScene.FlightEnded -= FlightSceneEnd;
        Game.Instance.FlightScene.Initialized -= FlightSceneInitalized;
        (Game.Instance.FlightScene.ViewManager as ViewManagerScript).ViewChanged -= ViewChanged;
    }

    private void ViewChanged(object sender, EventArgs e) => ManageViewVisibility(Game.Instance.FlightScene.ViewManager.GameView.RenderView);

    internal void ManageViewVisibility(bool gameView)
    {
        if (VisibilityState.HasValue)
        {
            if (_inspector != null && _inspector.IsPinned)
                Mod.Instance.UpdateInspectorPrefs(_inspector, mapOpened);
            _shouldSaveState = false;
            bool inspectorVisbility = gameView ? VisibilityState.Value.x != 0 : VisibilityState.Value.y != 0;
            FlightInfoVisible = inspectorVisbility;
            return;
        }
    }

    private void ManagePin(IInspectorPanel panel) => Mod.Instance.UpdateInspectorPrefs(_inspector, mapOpened);

    private void ManageMapUnpin(IInspectorPanel panel)
    {
        if (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.Visible)
        {
            Mod.Instance.UpdateInspectorPrefs(_inspector, openState);
            FlightInfoVisible = false;
        }
    }

    internal void UpdateNestedInspectors(CraftNode playerCraft)
    {
        FuelInfo.UpdateInspectors();
        //craftInfo.UpdateValues();
    }

    internal void UpdateInspectors(GameViewInspectorScript instance)
    {
        foreach (ButtonInspector buttonInspector in buttonInspectors)
        {
            if (buttonInspector._groupModelInspector != null)
            {
                GameViewInspectorViewModel _viewModel = (GameViewInspectorViewModel)Traverse.Create(instance).Field("_viewModel").GetValue();
                _viewModel.Update(instance.PlayerCraft);
                UpdateNestedInspectors(instance.PlayerCraft);
                break;
            }
        }
        if (craftInfo != null && craftInfo.GroupModel.Visible)
        {
            craftInfo.UpdateValues();
        }
    }
}
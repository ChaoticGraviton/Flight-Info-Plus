using Assets.Scripts;
using ModApi.Craft.Propulsion;
using ModApi.Flight;
using ModApi.Flight.Events;
using ModApi.Levels;
using ModApi.Ui.Inspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Xml;
using UnityEngine;

public class MapInfoPlus
{
    public IList<GroupModel> MapInfoGroups;
    internal IInspectorPanel MapInfoPanel;
    public XmlElement FlightInspectorInfoButton;
    internal InspectorModel MapInfoInspectorModel;

    // Main Flight Info Plus Inspector
    internal InspectorModel _inspectorModel;
    public IInspectorPanel _inspector;
    private Vector2? _currentOffset;
    private InspectorPanelCreationInfo creationInfo;
    public List<ButtonInspector> buttonInspectors;
    public bool _visible;

    internal void Initialize(InspectorModel mapInfoModel)
    {
        MapInfoInspectorModel = mapInfoModel;
        MapInfoGroups = mapInfoModel.Groups;
        MapInfoPanel = mapInfoModel.Panel;

        _inspectorModel = new InspectorModel("MapInfoPlus", "Map Info+");
        buttonInspectors = new List<ButtonInspector>();
        foreach (GroupModel group in MapInfoGroups)
        {
            if (group.Name != null)
            {
                buttonInspectors.Add(new ButtonInspector());
                ButtonInspector currentInstance = buttonInspectors.Last();
                currentInstance._modelName = group.Name;
                currentInstance._ItemModels = (List<ItemModel>)group.Items;
                currentInstance.Initialize();
                _inspectorModel.Add(new TextButtonModel(group.Name, new Action<TextButtonModel>(currentInstance.OnToggleButtonClicked)));
            }
        }
        Game.Instance.FlightScene.FlightEnded += FlightSceneEnd;
        Game.Instance.FlightScene.Initialized += FlightSceneLoaded;
    }

    // Override Inspectors
    private void FlightSceneEnd(object sender, FlightEndedEventArgs e)
    {
        _visible = false;
        _inspector = null;
    }
    private void FlightSceneLoaded(IFlightScene initializedObject)
    {
        if (MapInfoPanel != null) MapInfoPanel.Close();
    }

    internal void OnMapInfoButtonClicked() => MapInfoVisible = !MapInfoVisible;

    public bool MapInfoVisible
    {
        get => _visible;
        set
        {
            if (value)
            {
                _visible = true;
                if (_inspector != null)
                    _inspector.Visible = true;
                else
                    CreateInspector();
            }
            else
                ClosePanel();
        }
    }

    private void CreateInspector()
    {
        InspetorCreationInfo();
        _inspector = Game.Instance.UserInterface.CreateInspectorPanel(_inspectorModel, creationInfo);
        _inspector.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
    }

    private void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel) => ClosePanel();

    private void InspetorCreationInfo()
    {
        creationInfo = new()
        {
            StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
            PanelMaxHeight = 20,
            Resizable = true,
            StartOffset = !_currentOffset.HasValue ? new Vector2(-170f, -90f) : _currentOffset.Value
        };
    }

    private void ClosePanel()
    {
        if (_inspector == null) return;
        _visible = false;
        _currentOffset = new Vector2?(_inspector.Position);
        _inspector.Close();
        _inspector = null;
    }
}

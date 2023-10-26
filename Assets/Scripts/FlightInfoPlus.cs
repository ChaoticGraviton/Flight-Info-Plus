using Assets.Scripts;
using ModApi;
using ModApi.Common.DebugUtils;
using ModApi.Math;
using ModApi.Scenes.Events;
using ModApi.Ui.Inspector;
using System;
using System.Collections.Generic;
using UI.Xml;
using UnityEditor;
using UnityEngine;

public class FlightInfoPlus : MonoBehaviour
{
    public static IList<GroupModel> FlightInfoGroups;
    internal static IInspectorPanel FlightInfoPanel;
    public static XmlElement FlightInfoButton;

    internal static IInspectorPanel _flightInfoPlusInspector;
    private static Vector2? _currentOffset;
    private static InspectorPanelCreationInfo creationInfo;

    public static void OnFlightInfoClicked() => Visible = !Visible;

    public static bool Visible
    {
        get => _flightInfoPlusInspector != null;
        set
        {
            if (value)
            {
                if (_flightInfoPlusInspector != null)
                {
                    return;
                }
                RefreshInspectorPanel();
                FlightInfoButton.AddClass("panel-button-icon-toggled");
                Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            }
            else
            {
                ClosePanel();
                FlightInfoButton.RemoveClass("panel-button-icon-toggled");
                Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
            }
        }
    }

    private static void OnSceneLoaded(object sender, SceneEventArgs e)
    {
        if (!Game.InFlightScene)
        {
            _flightInfoPlusInspector = null;
        }
    }

    private static void RefreshInspectorPanel()
    {
        ClosePanel();
        InspetorCreationInfo();
        InspectorModel inspectorModel = new InspectorModel("FlightInfoPlus", "Flight Info+");

        foreach (GroupModel group in FlightInfoGroups)
        {
            if (group.Name != null)
            {
                //inspectorModel.Add(new TextButtonModel(group.Name, new Action<TextButtonModel>(CreateButtonInspector.OnButtonClicked)));
                inspectorModel.Add(new TextButtonModel(group.Name, new Action<TextButtonModel>(CreateButtonInspector.OnButtonClicked)));
            }
        }

        _flightInfoPlusInspector = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, creationInfo);
        _flightInfoPlusInspector.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
    }

    private static void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel) => ClosePanel();

    private static void InspetorCreationInfo()
    {
        creationInfo = new()
        {
            StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
            Resizable = true,
            StartOffset = !_currentOffset.HasValue ? new Vector2(-170f, -90f) : _currentOffset.Value
        };
    }

    private static void ClosePanel()
    {
        if (_flightInfoPlusInspector == null) return;
        _currentOffset = new Vector2?(_flightInfoPlusInspector.Position);
        _flightInfoPlusInspector.Close();
        _flightInfoPlusInspector = null;
    }
}

public class CreateButtonInspector
{
    internal static IInspectorPanel _groupModelInspector;
    private static Vector2? _currentOffset;
    private static InspectorPanelCreationInfo creationInfo;
    internal static IGroupModel _groupModel;
    internal static IList<GroupModel> _flightInfoGroups;
    internal static string _PreviousModelName;
    internal static string _modelName;

    public static void OnButtonClicked(TextButtonModel model)
    {
        GetLabelGroupModel(model.Label);
    }

    private static void GetLabelGroupModel(string label)
    {
        if (_groupModelInspector == null)
        {
            _flightInfoGroups = FlightInfoPlus.FlightInfoGroups;
            UpdateGroupModels(label);
        }
        Visible = !Visible;
    }

    private static void UpdateGroupModels(string label)
    {
        foreach (IGroupModel groupModel in _flightInfoGroups)
        {
            if (!string.IsNullOrEmpty(groupModel.Name))
            {
                if (groupModel.Name == label)
                {
                    _groupModel = groupModel;
                    Debug.Log("groupModel: " + _groupModel.Name + ", with " + _groupModel.Items.Count + " ItemElements");
                }
            }
        }
    }

    public static bool Visible
    {
        get => _groupModelInspector != null;
        set
        {
            if (value)
            {
                if (_groupModelInspector != null)
                {
                    return;
                }
                RefreshInspectorPanel();
                Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
            }
            else
            {
                ClosePanel();
                Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
            }
        }
    }

    private static void OnSceneLoaded(object sender, SceneEventArgs e)
    {
        if (!Game.InFlightScene)
        {
            _groupModelInspector = null;
        }
    }

    private static void RefreshInspectorPanel()
    {
        ClosePanel();
        InspetorCreationInfo();
        InspectorModel inspectorModel = new InspectorModel(_groupModel.Name + "-ID", _groupModel.Name);

        foreach (ItemModel item in _groupModel.Items)
        {            
            inspectorModel.Add(item);
        }

        _groupModelInspector = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, creationInfo);
        _groupModelInspector.CloseButtonClicked += new InspectorPanelDelegate(OnInspectorPanelCloseButtonClicked);
    }

    private static void OnInspectorPanelCloseButtonClicked(IInspectorPanel panel) => ClosePanel();

    private static void InspetorCreationInfo()
    {
        creationInfo = new()
        {
            StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.Center,
            Resizable = true,
            StartOffset = !_currentOffset.HasValue ? new Vector2(-170f, -90f) : _currentOffset.Value
        };
    }

    private static void ClosePanel()
    {
        if (_groupModelInspector == null) return;
        _currentOffset = new Vector2?(_groupModelInspector.Position);
        _groupModelInspector.Close();
        _groupModelInspector = null;
    }
}
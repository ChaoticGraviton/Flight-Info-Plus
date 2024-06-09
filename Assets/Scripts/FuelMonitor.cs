using Assets.Scripts;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Ui.Inspector;
using Rewired.Demos;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Xml;
using UnityEngine;
using UnityEngine.UI;

/*
// TODO //

    Fix the highlighting color 
    
    Change how to manage the refreshing of the fuel inspector so that the highlights don't break when a fuel is lost in the inspector, and a fuel is highlighed

// TODO //
*/

public class FuelInspectorMonitor : MonoBehaviour
{
    public FuelType FuelType;
    public IFuelSource[] FuelSources;
    IEnumerable<FuelTankData> FuelTanks;
    internal string _display = "UnsetDisplay";
    internal string _consumptionRate = "UnsetRate";
    internal double _fuelTotal = 0;
    internal double _fuelCapacity = 0;
    internal double _previousFuelValue = 0;
    public bool _isEnabled = false;
    internal bool _isBattery = false;
    internal Action OnMouseEnterAction;
    internal Action OnMouseExitAction;

    private ICraftScript _craftScript;

    private bool _isHighlightActive = false;

    public void Initialize()
    {
        OnMouseEnterAction -= MouseEnter;
        OnMouseEnterAction += MouseEnter;

        OnMouseExitAction -= MouseExit;
        OnMouseExitAction += MouseExit;

        Game.Instance.FlightScene.CraftStructureChanged += CraftStructureChanged;

        UpdateDisplay();
        if (!_isEnabled)
            return;
        var _itemModel = Mod.Instance.flightInfoPlus.fuelInspectorInstance._ItemModels;

        TextModel textModel = (new TextModel(FuelType.Name, () => _consumptionRate));
        //textModel.ElementCreated += ProgressBarCreated;
        _itemModel.Add(textModel);

        ProgressBarModel progressBarModel = new(() => _display, () => (float)(_fuelTotal / _fuelCapacity));
        progressBarModel.ElementCreated += ProgressBarCreated;

        _itemModel.Add(progressBarModel);
    }

    private void ProgressBarCreated(IItemElement element)
    {
        //element.GameObject.AddComponent<BoxCollider2D>();
        if (element.GameObject.TryGetComponent(out XmlElement xmlElement))
        {
            Debug.Log("Found XmlElement");
            xmlElement.GetComponentsInChildren<Image>();
            Image barImage = xmlElement.GetElementByInternalId<Image>("image-bar");
            barImage.raycastTarget = true;
            xmlElement.AddOnMouseEnterEvent(OnMouseEnterAction);
            xmlElement.AddOnMouseExitEvent(OnMouseExitAction);
        }
    }

    internal void UpdateDisplay()
    {
        if (!_isEnabled)
            return;
        GetFuelsSum();
        if (!Game.Instance.FlightScene.TimeManager.Paused)
        {
            var tempRate = (_fuelTotal - _previousFuelValue) / Game.Instance.FlightScene.TimeManager.DeltaTime;
            if (double.IsNaN(tempRate) || double.IsInfinity(tempRate))
                return;
            _consumptionRate = Mod.Instance.FormatMass(tempRate, _isBattery) + "/s";
        }
        _previousFuelValue = _fuelTotal;
        _display = Mod.Instance.FormatMass(_fuelTotal, _isBattery) + "/" + Mod.Instance.FormatMass(_fuelCapacity, _isBattery);
    }

    internal void GetFuelsSum()
    {
        double multiplier = _isBattery ? 1 : FuelType.Density;
        double sumTotal = 0;
        double sumCapacity = 0;
        foreach (var fuelSource in FuelSources)
        {
            if ((bool)(fuelSource?.IsDestroyed))
                continue;
            sumTotal += fuelSource.TotalFuel * multiplier;
            sumCapacity += fuelSource.TotalCapacity * multiplier;
        }
        _fuelTotal = sumTotal;
        _fuelCapacity = sumCapacity;
        _isEnabled = _fuelCapacity != 0;
    }

    public bool HighlightSources
    {
        get => _isHighlightActive;
        set
        {
            _isHighlightActive = value;
            SetFuelSourcesHighlight(_isHighlightActive);
        }
    }

    private void MouseExit()
    {
        if (HighlightSources)
        {
            HighlightSources = false;
        }
    }

    private void MouseEnter()
    {
        if (!HighlightSources)
            HighlightSources = true;
    }

    public void SetFuelSourcesHighlight(bool setHighlightState)
    {
        UpdateFuelTanks();
        if (FuelTanks == null)
        {
            Debug.Log("FuelTanks is null, returning");
            return;
        }
        foreach (FuelTankData tankData in FuelTanks)
        {
            IPartMaterialScript partMaterialScript = tankData.Part.PartScript.PartMaterialScript;
            partMaterialScript.IsHighlighted = setHighlightState;
        }
    }

    private void CraftStructureChanged()
    {
        // Disables the highlight when the craft structure changes
        MonitorRoutine fuelMonitorCoroutine = Mod.Instance.flightInfoPlus.fuelMonitorCoroutine;
        fuelMonitorCoroutine.StartCoroutine((IEnumerator)fuelMonitorCoroutine.SetFuelSourceHighlight(this));
    }

    public void GetPartsWithFuelType()
    {
        // Sets the craftscript
        _craftScript = Game.Instance.FlightScene.CraftNode.CraftScript;
        // Checks if the highlight in currently enabled
        if (HighlightSources)
        {
            MonitorRoutine fuelMonitorCoroutine = Mod.Instance.flightInfoPlus.fuelMonitorCoroutine;
            fuelMonitorCoroutine.StartCoroutine((IEnumerator)fuelMonitorCoroutine.SetFuelSourceHighlight(this));
            fuelMonitorCoroutine.StartCoroutine((IEnumerator)fuelMonitorCoroutine.UpdateFuelTanksLate(this));
            SetFuelSourcesHighlight(true);
            return;
        }
        UpdateFuelTanks();
    }

    public void UpdateFuelTanks() => FuelTanks = _craftScript?.Data.Assembly.GetModifiers<FuelTankData>().Where(fuelTank => fuelTank != null && fuelTank.FuelType == FuelType && fuelTank.Script.CraftFuelSource.SupportsFuelTransfer && fuelTank.Part.PartScript != null);    
}
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;
using ModApi.Ui.Inspector;
using System.Collections.Generic;
using UI.Xml;
using UnityEngine.UI;

namespace Assets.Scripts.Flight.Ui.FlightInfo.FuelInfo
{
    public class FuelInfoItemModel
    {
        private IFuelSource[] FuelSources;
        private List<FuelTankData> FuelTanks;
        private TextModel TextModel;
        private ProgressBarModel ProgressBarModel;
        private readonly FuelType FuelType;
        private readonly float fuelDensity = 1;
        private readonly string[] stringFormat;
        private string _progressDisplay = "UnsetDisplay";
        private string _consumptionRate = "UnsetRate";
        private double _fuelTotal = 0;
        private double _fuelCapacity = 0;
        private double _previousFuelValue = 0;
        private bool _isEnabled = false;
        private bool _highlightSources = false;

        public bool HighlightSources
        {
            get => _highlightSources;
            set
            {
                _highlightSources = value;
                SetHighlight(value);
            }
        }

        public FuelInfoItemModel(FuelType fueltype)
        {
            FuelType = fueltype;
            bool isBattery = fueltype.Name == "Battery";
            fuelDensity = isBattery ? 1 : FuelType.Density;
            stringFormat = isBattery ? Mod.Instance._energyTypes : Mod.Instance._massTypes;
            InitializeItemModels();
            Game.Instance.FlightScene.CraftStructureChanged += OnCraftStructureChanged;
            Game.Instance.FlightScene.FlightEnded += OnFlightEnded;
        }

        public ProgressBarModel GetProgressBarModel() => ProgressBarModel;
        public TextModel GetTextModel() => TextModel;
        public bool GetEnabled() => _isEnabled;
        public void SetFuelSources(IFuelSource[] sources) => FuelSources = sources;
        public void SetFuelTanks(List<FuelTankData> fuelTanks) => FuelTanks = fuelTanks;
        public void ClearHighlights() => SetHighlight(false);

        public void SetFuelValues()
        {
            double sumFuelTotal = 0;
            double sumFuelCapacity = 0;
            foreach (var fuelSource in FuelSources)
            {
                if ((bool)(fuelSource?.IsDestroyed))
                    continue;
                sumFuelTotal += fuelSource.TotalFuel * fuelDensity;
                sumFuelCapacity += fuelSource.TotalCapacity * fuelDensity;
            }
            _fuelTotal = sumFuelTotal;
            _fuelCapacity = sumFuelCapacity;
            _isEnabled = _fuelCapacity != 0;
        }
        public void InitializeItemModels()
        {
            TextModel = new(FuelType.Name, () => _consumptionRate);
            ProgressBarModel = new(() => _progressDisplay, () => (float)(_fuelTotal / _fuelCapacity));
            ProgressBarModel.ElementCreated += PopulateProgressBarEvents;
        }

        public void UpdateDisplay()
        {
            if (!_isEnabled) return;
            SetFuelValues();
            if (!Game.Instance.FlightScene.TimeManager.Paused)
            {
                var tempRate = (_fuelTotal - _previousFuelValue) / Game.Instance.FlightScene.TimeManager.DeltaTime;
                if (double.IsNaN(tempRate) || double.IsInfinity(tempRate))
                    return;
                _consumptionRate = $"{Mod.Instance.FormatFuel(tempRate, stringFormat)}/s";
            }
            _previousFuelValue = _fuelTotal;
            _progressDisplay = Mod.Instance.FormatFuel(_fuelTotal, stringFormat) + "/" + Mod.Instance.FormatFuel(_fuelCapacity, stringFormat);
        }

        private void PopulateProgressBarEvents(IItemElement element)
        {
            if (ProgressBarModel.ItemElement.GameObject.TryGetComponent(out XmlElement xmlElement) && ProgressBarModel != null)
            {
                Image barImage = xmlElement.GetElementByInternalId<Image>("image-bar");
                if (barImage != null) barImage.raycastTarget = true;
                xmlElement.AddOnMouseEnterEvent(() => HighlightSources = true);
                xmlElement.AddOnMouseExitEvent(() => HighlightSources = false);
                xmlElement.AddOnHideEvent(() => HighlightSources = false);
            }
        }

        private void OnCraftStructureChanged() => ClearHighlights();
        private void OnFlightEnded(object sender, ModApi.Flight.Events.FlightEndedEventArgs e)
        {
            Game.Instance.FlightScene.CraftStructureChanged -= OnCraftStructureChanged;
            Game.Instance.FlightScene.FlightEnded -= OnFlightEnded;
        }

        private void SetHighlight(bool value)
        {
            if (FuelTanks != null)
                foreach (FuelTankData tankData in FuelTanks)
                    SetMaterialScriptHighlight(tankData.Part.PartScript, value);
        }

        private void SetMaterialScriptHighlight(IPartScript partScript, bool value)
        {
            IPartMaterialScript partMaterialScript = partScript.PartMaterialScript;
            if (partMaterialScript != null)
                try
                {
                    partMaterialScript.IsHighlighted = value;
                }
                catch { }
        }
    }
}
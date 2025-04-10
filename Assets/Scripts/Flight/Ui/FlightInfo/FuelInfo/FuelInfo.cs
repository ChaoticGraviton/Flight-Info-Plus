using Assets.Scripts.Flight.Ui.FlightInfo.Tools;
using ModApi.Craft.Parts;
using ModApi.Craft;
using ModApi.Craft.Propulsion;
using ModApi.Ui.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ModApi.Flight;
using ModApi.Flight.Events;
using Assets.Scripts.Craft.Parts.Modifiers;

namespace Assets.Scripts.Flight.Ui.FlightInfo.FuelInfo
{
    public class FuelInfo
    {
        private FuelRoutines fuelRoutines;
        internal Dictionary<FuelType, FuelInfoItemModel> FuelInfoItemModels;
        private InspectorModel inspector;
        internal ButtonInspector buttonInspector;

        public FuelInfo(ButtonInspector buttonInspector)
        {
            this.buttonInspector = buttonInspector;
            fuelRoutines = new GameObject().AddComponent<FuelRoutines>();
            FuelInfoItemModels = new Dictionary<FuelType, FuelInfoItemModel>();
            foreach (FuelType fuel in Game.Instance.PropulsionData.Fuels)
            {
                FuelInfoItemModels.Add(fuel, new FuelInfoItemModel(fuel));
            }
            Game.Instance.FlightScene.CraftStructureChanged += CraftStructureChanged;
            Game.Instance.FlightScene.CraftChanged += CraftChanged;
            Game.Instance.FlightScene.Initialized += FlightSceneInitalized;
            Game.Instance.FlightScene.FlightEnded += FlightSceneEnded;
        }

        internal void ConfigFuelMonitors()
        {
            this.inspector ??= buttonInspector._inspectorModel;
            var sources = GetSortedFuelSources();

            // Pass the data onto the fuel inspectors
            RemoveGroups();
            foreach (var source in sources)
            {
                FuelInfoItemModel instance = FuelInfoItemModels[source.Item1];
                instance.ClearHighlights();
                instance.SetFuelSources(source.Item2);
                instance.SetFuelValues();
                instance.SetFuelTanks(GetFuelTypeTanks(source.Item1));
                instance.UpdateDisplay();
                if (instance.GetEnabled())
                {
                    inspector.Add(instance.GetTextModel());
                    inspector.Add(instance.GetProgressBarModel());
                }
            }
            inspector.Add(Mod.Instance.FlightInfoPlus.InspectorSpacerModel);
            inspector.Panel?.RebuildModelElements();
        }

        private List<FuelTankData> GetFuelTypeTanks(FuelType fuelType) => Game.Instance.FlightScene.CraftNode.CraftScript?.Data.Assembly.GetModifiers<FuelTankData>().Where(fuelTank => fuelTank != null && fuelTank.FuelType == fuelType && fuelTank.Script.CraftFuelSource.SupportsFuelTransfer && fuelTank.Part.PartScript != null).ToList();

        Tuple<FuelType, IFuelSource[]>[] GetSortedFuelSources()
        {
            ICraftScript craft = Game.Instance.FlightScene.CraftNode.CraftScript;
            var fuelSources = new Dictionary<FuelType, List<IFuelSource>>();
            var noneFuelType = Game.Instance.PropulsionData.GetFuelType("None");

            foreach (var source in craft.FuelSources.FuelSources)
            {
                FuelType type = source.FuelType;
                if (type == noneFuelType || !source.SupportsFuelTransfer || source.TotalCapacity == 0) continue;

                if (!fuelSources.ContainsKey(type))
                    fuelSources.Add(type, new List<IFuelSource>());

                fuelSources[type].Add(source);
            }

            var sourcesArray = new Tuple<FuelType, IFuelSource[]>[fuelSources.Count];

            int index = 0;
            foreach (var sources in fuelSources)
                sourcesArray[index++] = new Tuple<FuelType, IFuelSource[]>(sources.Value[0].FuelType, sources.Value.ToArray());

            return sourcesArray.OrderBy(i => i.Item1.Name).ToArray();
        }

        private void RemoveGroups()
        {
            var items = inspector.Items;
            if (items.Count < 1)
            {
                Debug.LogWarning("Cannot remove group(s), _inspectorModel does not contain any items.");
                return;
            }
            inspector.RemoveGroup((GroupModel)items[0]);
        }

        internal void UpdateInspectors()
        {
            foreach (var monitor in FuelInfoItemModels)
                monitor.Value.UpdateDisplay();
        }

        private void CraftChanged(ICraftNode craftNode)
        {
            if (Game.InFlightScene)
                fuelRoutines.StartCoroutine(fuelRoutines.ConfigFuelMonitors());
        }

        private void CraftStructureChanged()
        {
            if (Game.InFlightScene)
                fuelRoutines.StartCoroutine(fuelRoutines.ConfigFuelMonitors());
        }

        private void FlightSceneInitalized(IFlightScene initializedObject) => fuelRoutines.StartCoroutine(fuelRoutines.ConfigFuelMonitors());

        private void FlightSceneEnded(object sender, FlightEndedEventArgs e)
        {
            Game.Instance.FlightScene.CraftStructureChanged -= CraftStructureChanged;
            Game.Instance.FlightScene.CraftChanged -= CraftChanged;
            Game.Instance.FlightScene.Initialized -= FlightSceneInitalized;
            Game.Instance.FlightScene.FlightEnded -= FlightSceneEnded;
        }
    }
}
using System;
using HarmonyLib;
using ModApi.Ui.Inspector;
using UnityEngine;

namespace Assets.Scripts
{
    public class Mod : ModApi.Mods.GameMod
    {
        public static Mod Instance { get; } = GetModInstance<Mod>();
        public readonly string[] _massTypes = { "g", "kg", "t", "kt" };
        public readonly string[] _energyTypes = { "J", "kJ", "MJ", "GJ" };

        public FlightInfoPlus FlightInfoPlus;
        public MapInfoPlus MapInfoPlus;

        protected override void OnModInitialized()
        {
            Harmony harmony = new("FlightInfo+");
            harmony.PatchAll();
            FlightInfoPlus = new FlightInfoPlus();
            MapInfoPlus = new MapInfoPlus();
            DisableStockInspectors();

            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.FlightView, OnBuildFlightViewInspectorPanel);
            //Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.MapView, OnBuildMapViewInspectorPanel);
        }

        private void DisableStockInspectors()
        {
            var settings = Game.Instance.Settings;
            settings.Game.Flight.ShowFlightViewInspector.UpdateAndCommit(false);
        }

        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            Debug.Log("Flight Info Build");
            FlightInfoPlus.Initialize(request.Model);
        }

        private void OnBuildMapViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            Debug.Log("Map Info Build");
            //mapInfoPlus.Initialize(request.Model);
        }

        public void UpdateInspectorPrefs(IInspectorPanel panel, Vector2 visibleState)
        {
            Game.Instance.Settings.UserPrefs.SetVector2(panel.Model.UserPrefsId + ".Visible", visibleState);
            Game.Instance.Settings.Save();
        }

        public string FormatFuel(double totalFuel, string[] format)
        {
            // Converts into lowest unit type
            totalFuel *= 1e3;
            if (Math.Abs(totalFuel) > 1e9)
                return (totalFuel * 1e-9).ToString("0.00") + format[3];
            else if (Math.Abs(totalFuel) > 1e6)
                return (totalFuel * 1e-6).ToString("0.00") + format[2];
            else if (Math.Abs(totalFuel) > 1e3)
                return (totalFuel * 1e-3).ToString("0.00") + format[1];
            return totalFuel.ToString("0.00") + format[0];
        }
    }
}
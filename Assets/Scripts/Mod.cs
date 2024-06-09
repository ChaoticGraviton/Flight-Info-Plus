namespace Assets.Scripts
{
    using System;
    using System.Collections;
    using Assets.Scripts.Flight.GameView.UI.Inspector;
    using Assets.Scripts.Flight.MapView;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Flight.UI;
    using HarmonyLib;
    using ModApi.Flight.GameView;
    using ModApi.Ui.Inspector;
    using UI.Xml;
    using UnityEngine;

    /* TODO // /

    // TODO // */

    public class Mod : ModApi.Mods.GameMod
    {
        public static Mod Instance { get; } = GetModInstance<Mod>();

        public FlightInfoPlus flightInfoPlus;
        public MapInfoPlus mapInfoPlus;

        internal string[] _massTypes = { "g", "kg", "t", "kt" };
        internal string[] _energyTypes = { "J", "kJ", "MJ", "GJ" };
        public string FormatMass(double totalFuel, bool battery = false)
        {
            string[] formatType = battery ? _energyTypes : _massTypes;
            // Converts into lowest unit type
            totalFuel *= 1e3;
            if (Math.Abs(totalFuel) > 1e9)
                return (totalFuel * 1e-9).ToString("0.00") + formatType[3];
            else if (Math.Abs(totalFuel) > 1e6)
                return (totalFuel * 1e-6).ToString("0.00") + formatType[2];
            else if (Math.Abs(totalFuel) > 1e3)
                return (totalFuel * 1e-3).ToString("0.00") + formatType[1];
            return totalFuel.ToString("0.00") + formatType[0];
        }

        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("FlightInfo+");
            harmony.PatchAll();
            flightInfoPlus = new FlightInfoPlus();
            mapInfoPlus = new MapInfoPlus();
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
            flightInfoPlus.Initialize(request.Model);
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

        [HarmonyPatch(typeof(NavPanelController))]
        class UpdateNavPanelPatch
        {
            public static XmlElement flightInfoButton;

            public static IGameView gameView;

            [HarmonyPatch("OnToggleFlightInspectorClicked")]
            static bool Prefix(NavPanelController __instance, XmlElement toggle)
            {
                //Need to consider the button also being used for map view                
                gameView = Game.Instance.FlightScene.ViewManager.GameView;
                flightInfoButton = __instance.xmlLayout.GetElementById("toggle-flight-inspector");
                Instance.flightInfoPlus.FlightInfoButton = flightInfoButton;
                Instance.mapInfoPlus.FlightInspectorInfoButton = flightInfoButton;

                if (gameView.RenderView)
                {
                    flightInfoButton.Tooltip = "Toggle Flight Info+ Panel";
                    Instance.flightInfoPlus.OnFlightInfoButtonClicked();
                }
                else
                {
                    /*
                    flightInfoButton.Tooltip = "Toggle Map Info+ Panel";
                    Instance.mapInfoPlus.OnMapInfoButtonClicked();
                     */
                    MapViewScript mapView = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView as MapViewScript;
                    mapView.MapViewUi.MapViewInspector.Visible = !mapView.MapViewUi.MapViewInspector.Visible;
                }
                return false;
            }

            [HarmonyPatch("UpdatePanel")]
            static void Postfix(NavPanelController __instance, CraftNode craftNode)
            {
                if (Instance.flightInfoPlus._inspector != null)
                {
                    Traverse.Create(__instance).Method("UpdateButton", __instance.xmlLayout.GetElementById("toggle-flight-inspector"), GetButtonVisibility()).GetValue();
                }
            }

            private static bool GetButtonVisibility()
            {
                // relpace the else with map view plus if implimented                
                return Game.Instance.FlightScene.ViewManager.GameView.RenderView ? Instance.flightInfoPlus.FlightInfoVisible : (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView as MapViewScript).MapViewUi.MapViewInspector.Visible;
            }
        }

        [HarmonyPatch(typeof(GameViewInspectorScript))]
        class UpdateFlightInfoPlusValuesPatch
        {
            [HarmonyPatch("Update")]
            static void Postfix(GameViewInspectorScript __instance)
            {
                Instance.flightInfoPlus.DetermineUpdates(__instance);
            }
        }
    }

    public class MonitorRoutine : MonoBehaviour
    {
        public IEnumerator FuelMonitorCoroutine()
        {
            yield return new WaitForSeconds(Time.deltaTime * 5);
            Mod.Instance.flightInfoPlus.ConfigFuelMonitors();
        }

        public IEnumerator FuelMonitorCoroutineTest(FlightInfoPlus instance)
        {
            yield return new WaitForSeconds(Time.deltaTime * 5);
            Mod.Instance.flightInfoPlus.ConfigFuelMonitors();
        }

        public IEnumerable SetFuelSourceHighlight(FuelInspectorMonitor fuelInspectorMonitor)
        {
            yield return new WaitForSeconds(Time.deltaTime * 5);
            fuelInspectorMonitor.SetFuelSourcesHighlight(false);
        }

        public IEnumerable UpdateFuelTanksLate(FuelInspectorMonitor fuelInspectorMonitor)
        {

            yield return new WaitForSeconds(Time.deltaTime * 5);
            fuelInspectorMonitor.UpdateFuelTanks();
        }
    }
}
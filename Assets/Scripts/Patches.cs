using Assets.Scripts.Flight.GameView.UI.Inspector;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.Flight.UI;
using HarmonyLib;
using ModApi.Flight.GameView;
using UI.Xml;

namespace Assets.Scripts
{
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
            Mod.Instance.FlightInfoPlus.FlightInfoButton = flightInfoButton;
            Mod.Instance.MapInfoPlus.FlightInspectorInfoButton = flightInfoButton;

            if (gameView.RenderView)
            {
                flightInfoButton.Tooltip = "Toggle Flight Info+ Panel";
                Mod.Instance.FlightInfoPlus.OnFlightInfoButtonClicked();
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
            if (Mod.Instance.FlightInfoPlus._inspector != null)
            {
                Traverse.Create(__instance).Method("UpdateButton", __instance.xmlLayout.GetElementById("toggle-flight-inspector"), GetButtonVisibility()).GetValue();
            }
        }

        private static bool GetButtonVisibility()
        {
            // relpace the else with map view plus if implimented                
            return Game.Instance.FlightScene.ViewManager.GameView.RenderView ? Mod.Instance.FlightInfoPlus.FlightInfoVisible :
                (Game.Instance.FlightScene.ViewManager.MapViewManager.MapView as MapViewScript).MapViewUi.MapViewInspector.Visible;
        }
    }

    [HarmonyPatch(typeof(GameViewInspectorScript))]
    class UpdateFlightInfoPlusValuesPatch
    {
        [HarmonyPatch("Update")]
        static void Postfix(GameViewInspectorScript __instance)
        {
            Mod.Instance.FlightInfoPlus.UpdateInspectors(__instance);
        }
    }
}
namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Assets.Scripts.Design.Tools.Fuselage;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Flight.GameView;
    using Assets.Scripts.Flight.GameView.UI;
    using Assets.Scripts.Flight.GameView.UI.Inspector;
    using Assets.Scripts.Flight.MapView.UI;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Flight.UI;
    using HarmonyLib;
    using ModApi;
    using ModApi.Common;
    using ModApi.Common.DebugUtils;
    using ModApi.Flight;
    using ModApi.Flight.GameView;
    using ModApi.Mods;
    using ModApi.Scenes.Events;
    using ModApi.Ui;
    using ModApi.Ui.Inspector;
    using UI.Xml;
    using UnityEngine;

    public class Mod : ModApi.Mods.GameMod
    {

        public static Mod Instance { get; } = GetModInstance<Mod>();

        protected override void OnModInitialized()
        {
            Harmony harmony = new Harmony("FlightInfo+");
            harmony.PatchAll();
            Game.Instance.UserInterface.AddBuildInspectorPanelAction(InspectorIds.FlightView, OnBuildFlightViewInspectorPanel);
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(object sender, SceneEventArgs e)
        {
            if (Game.InFlightScene)
            {
                if (FlightInfoPlus.FlightInfoPanel != null) FlightInfoPlus.FlightInfoPanel.Close();
            }
            else FlightInfoPlus._flightInfoPlusInspector = null;
        }

        private void OnBuildFlightViewInspectorPanel(BuildInspectorPanelRequest request)
        {
            FlightInfoPlus.FlightInfoGroups = request.Model.Groups;
            FlightInfoPlus.FlightInfoPanel = request.Model.Panel;
        }

        [HarmonyPatch(typeof(NavPanelController))]
        class UpdateNavPanelPatch
        {
            public static XmlElement flightInfoButton;

            [HarmonyPatch("OnToggleFlightInspectorClicked")]
            static bool Prefix(NavPanelController __instance, XmlElement toggle)
            {
                flightInfoButton = __instance.xmlLayout.GetElementById("toggle-flight-inspector");
                flightInfoButton.Tooltip = "Toggle Flight Info+ Panel";
                FlightInfoPlus.FlightInfoButton = flightInfoButton;
                FlightInfoPlus.OnFlightInfoClicked();
                return false;
            }

            [HarmonyPatch("UpdatePanel")]
            static void Postfix(NavPanelController __instance, CraftNode craftNode)
            {
                Traverse.Create(__instance).Method("UpdateButton", flightInfoButton, FlightInfoPlus.Visible).GetValue();
            }
        }
    }
}
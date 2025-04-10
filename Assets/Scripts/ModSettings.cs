namespace Assets.Scripts
{
    using ModApi.Settings.Core;

    public class ModSettings : SettingsCategory<ModSettings>
    {

        private static ModSettings _instance;

        public ModSettings() : base("Flight Info Plus")
        {
        }

        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        protected override void InitializeSettings()
        {
        }

    }
}
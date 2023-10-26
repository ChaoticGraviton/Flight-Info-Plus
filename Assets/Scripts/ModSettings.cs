namespace Assets.Scripts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Common;
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
    public class ModSettings : SettingsCategory<ModSettings>
    {

        private static ModSettings _instance;

        public ModSettings() : base("Flight Info Plus")
        {
        }

        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        protected override void InitializeSettings()
        {
            //this.TestSetting1 = this.CreateNumeric<float>("Test Setting 1", 1f, 10f, 1f)
            //    .SetDescription("A test setting that does nothing.")
            //    .SetDisplayFormatter(x => x.ToString("F1"))
            //    .SetDefault(2f);
        }
    }
}
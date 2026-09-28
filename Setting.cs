// Setting.cs
using System.Collections.Generic;
using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using Game.UI;

namespace ElectricCarSubsidy
{
    [FileLocation(nameof(ElectricCarSubsidy))]
    [SettingsUIGroupOrder(kSliderGroup)]
    [SettingsUIShowGroupName(kSliderGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string kSliderGroup = "Slider";

        public Setting(IMod mod) : base(mod)
        {
        }

        // Coût moyen du véhicule électrique, paramétrable par le joueur
        [SettingsUISlider(min = 20000, max = 45000, step = 1000, scalarMultiplier = 1, unit = Unit.kMoney)]
        [SettingsUISection(kSection, kSliderGroup)]
        public int VehicleCost { get; set; }

        // Bonus de bien-être appliqué pendant que le trait "Mobilité électrique
        // subventionnée" est actif
        [SettingsUISlider(min = 0, max = 50, step = 1, scalarMultiplier = 1, unit = Unit.kInteger)]
        [SettingsUISection(kSection, kSliderGroup)]
        public int WellbeingBonusValue { get; set; }

        public override void SetDefaults()
        {
            VehicleCost = 35000;
            WellbeingBonusValue = 15;
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "ElectricCarSubsidy" },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kSliderGroup), "Sliders" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VehicleCost)), "Vehicle cost" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VehicleCost)), "Average cost of an electric vehicle, used to calculate subsidy amounts" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.WellbeingBonusValue)), "Wellbeing bonus" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.WellbeingBonusValue)), "Wellbeing bonus applied to a district while the \"Subsidized electric mobility\" trait is active" },
            };
        }

        public void Unload()
        {
        }
    }
}
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using ElectricCarSubsidy.Systems;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace ElectricCarSubsidy
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(ElectricCarSubsidy)}.{nameof(Mod)}").SetShowsErrorsInUI(false);

        public static Mod Instance { get; private set; }
        public Setting Settings => m_Setting;

        private Setting m_Setting;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            Instance = this;

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));

            AssetDatabase.global.LoadSettings(nameof(ElectricCarSubsidy), m_Setting, new Setting(this));

            updateSystem.UpdateAt<EVSubsidyPolicyRegistry>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyPolicySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyEligibilityCheckSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyTraitMonitorSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVAdoptionProgressSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyWellbeingApplySystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyTraitAwardSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyDeadlineSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyUISystem>(SystemUpdatePhase.UIUpdate);
            //updateSystem.UpdateAt<EVSubsidyDebugHarnessSystem>(SystemUpdatePhase.GameSimulation); // temporaire, pour ce test

            // Dumps (déjà présents, on les garde pour l'instant)
            updateSystem.UpdateAt<EVSubsidyDebugDumpSystem>(SystemUpdatePhase.GameSimulation);
            updateSystem.UpdateAt<EVSubsidyResidentDumpSystem>(SystemUpdatePhase.GameSimulation);
            //updateSystem.UpdateAt<EVSubsidyWellbeingDumpSystem>(SystemUpdatePhase.GameSimulation);
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
            Instance = null;
        }
    }
}
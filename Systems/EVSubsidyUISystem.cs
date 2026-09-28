// Systems/EVSubsidyUISystem.cs
using Colossal.Entities;
using Colossal.UI.Binding;
using ElectricCarSubsidy.Components;
using ElectricCarSubsidy.Utils;
using Game.Areas;
using Game.City;
using Game.Simulation;
using Game.Tools;
using Game.UI;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyUISystem : UISystemBase
    {
        private const string kGroup = "evSubsidy";

        private ToolSystem m_ToolSystem;
        private EntityManager m_EntityManager;
        private TimeSystem m_TimeSystem;
        private EVSubsidyApplySystem m_ApplySystem;
        private EVSubsidyPolicyRegistry m_Registry;
        private RawValueBinding m_StateBinding;
        private int m_LastApplyToken = 0;
        private bool m_LastApplySuccess = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ToolSystem = World.GetOrCreateSystemManaged<ToolSystem>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_ApplySystem = World.GetOrCreateSystemManaged<EVSubsidyApplySystem>();
            m_Registry = World.GetOrCreateSystemManaged<EVSubsidyPolicyRegistry>();
            m_EntityManager = World.EntityManager;

            AddUpdateBinding(new GetterValueBinding<bool>(kGroup, "panelVisible", HasActiveSubsidyState));
            AddUpdateBinding(m_StateBinding = new RawValueBinding(kGroup, "subsidyState", WriteState));
            AddUpdateBinding(new GetterValueBinding<int>(kGroup, "lastApplyToken", () => m_LastApplyToken));
            AddUpdateBinding(new GetterValueBinding<bool>(kGroup, "lastApplySuccess", () => m_LastApplySuccess));

            AddBinding(new TriggerBinding<Entity, string>(kGroup, "applySubsidy", ApplySubsidy));
        }

        private bool HasActiveSubsidyState()
        {
            Entity selected = m_ToolSystem.selected;
            if (selected == Entity.Null || !m_EntityManager.Exists(selected)
                || !m_EntityManager.HasComponent<District>(selected)
                || !m_EntityManager.HasComponent<DistrictEVSubsidyState>(selected))
            {
                return false;
            }

            var state = m_EntityManager.GetComponentData<DistrictEVSubsidyState>(selected);

            return EVSubsidyPolicyUtils.IsCombustionBanActive(m_EntityManager, m_Registry, selected)
                || state.DistrustTraitActive || state.AngerTraitActive || state.TraitActive;
        }

        private void WriteState(IJsonWriter writer)
        {
            Entity selected = m_ToolSystem.selected;
            bool hasState = selected != Entity.Null && m_EntityManager.Exists(selected)
                && m_EntityManager.HasComponent<DistrictEVSubsidyState>(selected);

            DistrictEVSubsidyState state = default;
            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);
            byte[] sliderValues = { 0, 0, 0, 0, 0 };
            float[] progressValues = { 0f, 0f, 0f, 0f, 0f };
            int[] eligibleCounts = { 0, 0, 0, 0, 0 };
            bool combustionBanActive = false;

            if (hasState)
            {
                state = m_EntityManager.GetComponentData<DistrictEVSubsidyState>(selected);

                if (m_EntityManager.HasComponent<EVSubsidySliderConfig>(selected))
                {
                    var cfg = m_EntityManager.GetComponentData<EVSubsidySliderConfig>(selected);
                    sliderValues[0] = cfg.Uneducated;
                    sliderValues[1] = cfg.PoorlyEducated;
                    sliderValues[2] = cfg.Educated;
                    sliderValues[3] = cfg.WellEducated;
                    sliderValues[4] = cfg.HighlyEducated;
                }

                if (m_EntityManager.HasComponent<EVAdoptionProgress>(selected))
                {
                    var prog = m_EntityManager.GetComponentData<EVAdoptionProgress>(selected);
                    progressValues[0] = prog.Uneducated;
                    progressValues[1] = prog.PoorlyEducated;
                    progressValues[2] = prog.Educated;
                    progressValues[3] = prog.WellEducated;
                    progressValues[4] = prog.HighlyEducated;
                }

                if (m_EntityManager.HasBuffer<EVAdultsSnapshot>(selected))
                {
                    var snapshot = m_EntityManager.GetBuffer<EVAdultsSnapshot>(selected);
                    for (int i = 0; i < snapshot.Length; i++)
                    {
                        if (snapshot[i].EducationTier < 5)
                            eligibleCounts[snapshot[i].EducationTier] = snapshot[i].AdultCount;
                    }
                }

                combustionBanActive = EVSubsidyPolicyUtils.IsCombustionBanActive(m_EntityManager, m_Registry, selected);
            }

            float vehicleCost = Mod.Instance.Settings.VehicleCost;
            float playerMoney = SystemAPI.HasSingleton<PlayerMoney>()
                ? SystemAPI.GetSingleton<PlayerMoney>().money
                : 0f;

            // Forme JSON TOUJOURS identique, que hasState soit vrai ou faux —
            // seules les valeurs changent (défauts si pas de state), jamais le nombre
            // ou l'ordre des propriétés, pour éviter un mismatch de schéma côté binding UI.
            writer.TypeBegin("evSubsidy.SubsidyState");

            writer.PropertyName("hasState");
            writer.Write(hasState);

            writer.PropertyName("policyApplied");
            writer.Write(state.PolicyApplied);

            writer.PropertyName("slidersLocked");
            writer.Write(state.SlidersLocked);

            writer.PropertyName("applyDeadline");
            writer.Write(state.ApplyDeadline);

            writer.PropertyName("lockExpiry");
            writer.Write(state.LockExpiry);

            writer.PropertyName("nextEligibilityCheck");
            writer.Write(state.NextEligibilityCheck);

            writer.PropertyName("currentDay");
            writer.Write(currentDay);

            writer.PropertyName("traitActive");
            writer.Write(state.TraitActive);

            writer.PropertyName("angerTraitActive");
            writer.Write(state.AngerTraitActive);

            writer.PropertyName("traitExpiry");
            writer.Write(state.TraitExpiry);

            writer.PropertyName("distrustTraitActive");
            writer.Write(state.DistrustTraitActive);

            writer.PropertyName("distrustTraitExpiry");
            writer.Write(state.DistrustTraitExpiry);

            writer.PropertyName("subsidizedAdultsCount");
            writer.Write(state.SubsidizedAdultsCount);

            writer.PropertyName("carriedOverRecipients");
            writer.Write(state.CarriedOverRecipients);

            writer.PropertyName("vehicleCost");
            writer.Write(vehicleCost);

            writer.PropertyName("playerMoney");
            writer.Write(playerMoney);

            writer.PropertyName("combustionBanActive");
            writer.Write(combustionBanActive);

            writer.PropertyName("sliders");
            writer.ArrayBegin(5);
            for (int i = 0; i < 5; i++) writer.Write(sliderValues[i]);
            writer.ArrayEnd();

            writer.PropertyName("progress");
            writer.ArrayBegin(5);
            for (int i = 0; i < 5; i++) writer.Write(progressValues[i]);
            writer.ArrayEnd();

            writer.PropertyName("eligibleCounts");
            writer.ArrayBegin(5);
            for (int i = 0; i < 5; i++) writer.Write(eligibleCounts[i]);
            writer.ArrayEnd();

            writer.TypeEnd();
        }




        private void ApplySubsidy(Entity district, string sliderCsv)
        {
            Mod.log.Info($"[UI] applySubsidy reçu : district={district}, csv brut='{sliderCsv}'");
            var parts = sliderCsv.Split(',');
            if (parts.Length != 5)
            {
                Mod.log.Warn($"[UI] applySubsidy : format CSV invalide '{sliderCsv}'");
                m_LastApplySuccess = false;
                m_LastApplyToken++;
                return;
            }

            var sliderConfig = new EVSubsidySliderConfig
            {
                Uneducated = byte.Parse(parts[0]),
                PoorlyEducated = byte.Parse(parts[1]),
                Educated = byte.Parse(parts[2]),
                WellEducated = byte.Parse(parts[3]),
                HighlyEducated = byte.Parse(parts[4])
            };

            bool success = m_ApplySystem.TryApplySubsidyPolicy(district, sliderConfig);
            m_LastApplySuccess = success;
            m_LastApplyToken++;
            Mod.log.Info($"[UI] Application demandée sur {district} : succès={success}");
        }
    }
}
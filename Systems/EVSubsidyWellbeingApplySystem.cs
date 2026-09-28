// Systems/EVSubsidyWellbeingApplySystem.cs
using Colossal.Entities;
using ElectricCarSubsidy.Components;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Logging;

namespace ElectricCarSubsidy.Systems
{
    // S'exécute après le recalcul natif du jeu, pour ajouter notre propre
    // contribution de mécontentement sans se faire écraser silencieusement.
    [UpdateAfter(typeof(Game.Policies.DistrictModifierInitializeSystem))]
    public partial class EVSubsidyWellbeingApplySystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        // Facteur de calibrage : à ajuster selon l'échelle réelle observée en jeu
        private const float kDeficitToWellbeingScale = 0.5f;

        private EntityQuery m_WellbeingQuery;
        

        protected override void OnCreate()
        {
            base.OnCreate();

            m_WellbeingQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadOnly<DistrictEVSubsidyState>(),
                ComponentType.ReadWrite<DistrictModifier>()
            );
            RequireForUpdate(m_WellbeingQuery);
        }

        protected override void OnUpdate()
        {
            var entities = m_WellbeingQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);

                float deficitContribution = 0f;
                if (EntityManager.HasComponent<EVSubsidyDeficit>(district))
                {
                    var deficit = EntityManager.GetComponentData<EVSubsidyDeficit>(district);
                    float weightedDeficit = GetWeightedDeficit(district, deficit);
                    deficitContribution = weightedDeficit < 0f ? weightedDeficit * kDeficitToWellbeingScale : 0f;
                }

                float bonusContribution = state.TraitActive ? Mod.Instance.Settings.WellbeingBonusValue : 0f;
        float delta = deficitContribution + bonusContribution;

                float previousDelta = state.LastAppliedWellbeingDelta;
                float diff = delta - previousDelta;

                if (diff != 0f)
                {
                    var modifiers = EntityManager.GetBuffer<DistrictModifier>(district);
                    int wellbeingIndex = (int)DistrictModifierType.Wellbeing;

                    while (modifiers.Length <= wellbeingIndex)
                        modifiers.Add(default(DistrictModifier));

                    var value = modifiers[wellbeingIndex];
                    value.m_Delta.x += diff;
                    modifiers[wellbeingIndex] = value;

                    state.LastAppliedWellbeingDelta = delta;
                    EntityManager.SetComponentData(district, state);

                    s_Log.Info($"[WELLBEING DEBUG] District {district} : deficit={deficitContribution}, bonus={bonusContribution}, delta={delta}, diff appliqué={diff}");
                }
            }

            entities.Dispose();
        }

        // Pondère le déficit de chaque tranche par sa proportion dans la population adulte totale
        private float GetWeightedDeficit(Entity district, EVSubsidyDeficit deficit)
        {
            if (!EntityManager.HasBuffer<EVAdultsSnapshot>(district)) return 0f;

            var snapshot = EntityManager.GetBuffer<EVAdultsSnapshot>(district);
            int totalAdults = 0;
            for (int i = 0; i < snapshot.Length; i++) totalAdults += snapshot[i].AdultCount;
            if (totalAdults <= 0) return 0f;

            short[] deficitValues =
            {
                deficit.Uneducated, deficit.PoorlyEducated,
                deficit.Educated, deficit.WellEducated, deficit.HighlyEducated
            };

            float weightedSum = 0f;
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].EducationTier >= 5) continue;
                float proportion = (float)snapshot[i].AdultCount / totalAdults;
                weightedSum += deficitValues[snapshot[i].EducationTier] * proportion;
            }

            return weightedSum;
        }
    }
}
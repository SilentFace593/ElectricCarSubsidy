// Systems/EVSubsidyTraitMonitorSystem.cs
using Colossal.Entities;
using Colossal.Logging;
using ElectricCarSubsidy.Components;
using ElectricCarSubsidy.Utils;
using Game;
using Game.Areas;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyTraitMonitorSystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EVSubsidyPolicyRegistry m_Registry;
        private EntityQuery m_TraitedDistrictsQuery;

        private float m_LastCheckDay = -1f;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_Registry = World.GetOrCreateSystemManaged<EVSubsidyPolicyRegistry>();

            m_TraitedDistrictsQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadWrite<DistrictEVSubsidyState>()
            );
            RequireForUpdate(m_TraitedDistrictsQuery);
        }

        protected override void OnUpdate()
        {
            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);

            if (currentDay == m_LastCheckDay) return;
            m_LastCheckDay = currentDay;

            var entities = m_TraitedDistrictsQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);

                if (!state.TraitActive) continue;
                if (!IsBanStillActive(district)) continue;

                int totalAdults = EVSubsidySnapshotUtils.GetTotalAdultsFromSnapshot(EntityManager, district);
                if (totalAdults <= 0) continue;

                float ratio = (float)state.SubsidizedAdultsCount / totalAdults;

                if (ratio < 0.5f)
                {
                    s_Log.Info($"District {district} : ratio subventionnés {ratio:P0} < 50%, perte du trait EV");

                    state.TraitActive = false;
                    state.TraitExpiry = 0f;

                    // Perdre l'avantage pendant que le bannissement est toujours actif
                    // rallume logiquement le mécontentement, sans repasser par tout le cycle "méfiants".
                    if (!state.AngerTraitActive)
                    {
                        state.AngerTraitActive = true;
                        s_Log.Info($"District {district} : trait 'Citoyens en colères' réactivé suite à la perte du trait positif");
                    }

                    EntityManager.SetComponentData(district, state);
                    ApplyDiscontentmentPenalty(district);
                }
            }

            entities.Dispose();
        }

        private bool IsBanStillActive(Entity district)
        {
            return EVSubsidyPolicyUtils.IsCombustionBanActive(EntityManager, m_Registry, district);
        }

        private void ApplyDiscontentmentPenalty(Entity district)
        {
            var deficit = new EVSubsidyDeficit
            {
                Uneducated = -100,
                PoorlyEducated = -70,
                Educated = -50,
                WellEducated = -30,
                HighlyEducated = -10
            };

            if (!EntityManager.HasComponent<EVSubsidyDeficit>(district))
                EntityManager.AddComponentData(district, deficit);
            else
                EntityManager.SetComponentData(district, deficit);

            s_Log.Info($"District {district} : pénalité de mécontentement maximale appliquée (trait perdu)");
        }
    }
}
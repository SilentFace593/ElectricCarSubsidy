// Systems/EVSubsidyDeadlineSystem.cs
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
    [UpdateAfter(typeof(EVSubsidyPolicySystem))]
    public partial class EVSubsidyDeadlineSystem : GameSystemBase
    {
        //paramètres de durée de blocage et du trait : 2f et 4f de base
        private const float kLockDurationDays = 0.05f;
        private const float kDistrustTraitDurationDays = 0.1f;

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EntityQuery m_ActiveQuery;
        private EVSubsidyPolicyRegistry m_Registry;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_Registry = World.GetOrCreateSystemManaged<EVSubsidyPolicyRegistry>();
            m_ActiveQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadWrite<DistrictEVSubsidyState>()
            );
            RequireForUpdate(m_ActiveQuery);
        }

        protected override void OnUpdate()
        {
            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);

            var entities = m_ActiveQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);
                bool dirty = false;

                // --- Cas 1 : deadline dépassée sans application ---
                // condition modifiée
                if (!state.PolicyApplied && !state.SlidersLocked && !state.DeadlinePenaltyApplied && currentDay >= state.ApplyDeadline)
                {
                    state.SlidersLocked = true;
                    state.LockExpiry = currentDay + kLockDurationDays;
                    state.DeadlinePenaltyApplied = true;  // empêche le retrigger
                    dirty = true;
                    ApplyMaxDeficit(district);
                }

                // --- Cas 2 : fin du blocage de 2 jours -> acquisition du trait négatif ---
                if (state.SlidersLocked && currentDay >= state.LockExpiry && !state.PolicyApplied)
                {
                    s_Log.Info($"District {district} : fin du blocage, sliders réouverts, trait négatif acquis (expire dans {kDistrustTraitDurationDays}j)");

                    state.SlidersLocked = false;
                    state.DistrustTraitActive = true;
                    state.DistrustTraitExpiry = currentDay + kDistrustTraitDurationDays;
                    dirty = true;
                }

                // --- Cas 3 : expiration naturelle du trait négatif "méfiants" ---
                if (state.DistrustTraitActive && currentDay >= state.DistrustTraitExpiry)
                {
                    state.DistrustTraitActive = false;
                    dirty = true;

                    if (IsBanStillActive(district))
                    {
                        // Le bannissement perdure : le mécontentement continue sous une forme prolongée
                        state.AngerTraitActive = true;
                        s_Log.Info($"District {district} : trait négatif expiré, bannissement toujours actif -> trait 'Citoyens en colères' activé");
                    }
                    else if (EntityManager.HasComponent<EVSubsidyDeficit>(district))
                    {
                        EntityManager.SetComponentData(district, new EVSubsidyDeficit());
                        s_Log.Info($"District {district} : trait négatif expiré, bannissement inactif -> déficit nettoyé");
                    }
                }

                // --- Cas 4 : "Citoyens en colères" se termine à la levée du bannissement ---
                if (state.AngerTraitActive && !IsBanStillActive(district))
                {
                    state.AngerTraitActive = false;
                    dirty = true;

                    if (EntityManager.HasComponent<EVSubsidyDeficit>(district))
                        EntityManager.SetComponentData(district, new EVSubsidyDeficit());

                    s_Log.Info($"District {district} : bannissement levé, trait 'Citoyens en colères' retiré, déficit nettoyé");
                }

                if (dirty)
                {
                    EntityManager.SetComponentData(district, state);
                }
            }

            entities.Dispose();
        }

        private bool IsBanStillActive(Entity district)
        {
            return EVSubsidyPolicyUtils.IsCombustionBanActive(EntityManager, m_Registry, district);
        }

        private void ApplyMaxDeficit(Entity district)
        {
            var maxDeficit = new EVSubsidyDeficit
            {
                Uneducated = -100,
                PoorlyEducated = -70,
                Educated = -50,
                WellEducated = -30,
                HighlyEducated = -10
            };

            if (!EntityManager.HasComponent<EVSubsidyDeficit>(district))
                EntityManager.AddComponentData(district, maxDeficit);
            else
                EntityManager.SetComponentData(district, maxDeficit);
        }
    }
}
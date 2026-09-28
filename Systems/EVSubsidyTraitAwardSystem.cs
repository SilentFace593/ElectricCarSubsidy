// Systems/EVSubsidyTraitAwardSystem.cs
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
    [UpdateAfter(typeof(EVAdoptionProgressSystem))]
    public partial class EVSubsidyTraitAwardSystem : GameSystemBase
    {
        // valeur de base kAwardThreshold = 0.95f;
        //paramètre par défaut de durée de trait récompense  : 15f
        private const float kAwardThreshold = 0.95f;
        private const float kTraitDurationDays = 0.2f;

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EntityQuery m_ActiveQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();

            m_ActiveQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadWrite<DistrictEVSubsidyState>(),
                ComponentType.ReadOnly<EVSubsidySliderConfig>()
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

                if (state.TraitActive) continue;
                if (!state.PolicyApplied) continue;
                if (state.AngerTraitActive) continue; // la colère doit d'abord retomber

                int totalAdults = EVSubsidySnapshotUtils.GetTotalAdultsFromSnapshot(EntityManager, district);
                if (totalAdults <= 0) continue;

                float ratio = (float)state.SubsidizedAdultsCount / totalAdults;
                s_Log.Info($"[TRAIT DIAG] District {district} : SubsidizedAdultsCount={state.SubsidizedAdultsCount}, totalAdults={totalAdults}, ratio={ratio:P1}");

                if (ratio < kAwardThreshold) continue;

                state.TraitActive = true;
                state.TraitExpiry = currentDay + kTraitDurationDays;
                EntityManager.SetComponentData(district, state);

                s_Log.Info($"District {district} : trait \"Mobilité électrique subventionnée\" décerné (ratio={ratio:P0}), expire jour {state.TraitExpiry}");
            }

            entities.Dispose();
        }

       
    }
}
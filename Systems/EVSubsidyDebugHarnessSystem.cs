// Systems/EVSubsidyDebugHarnessSystem.cs
using Colossal.Entities;
using Colossal.Logging;
using ElectricCarSubsidy.Components;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    // TEMPORAIRE : simule un clic "appliquer" avec des valeurs fixes,
    // pour tester la chaîne complète sans UI. À retirer une fois l'UI prête.
    [UpdateAfter(typeof(EVSubsidyEligibilityCheckSystem))]
    public partial class EVSubsidyDebugHarnessSystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private EntityQuery m_UnappliedStateQuery;
        private EVSubsidyApplySystem m_ApplySystem;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_ApplySystem = World.GetOrCreateSystemManaged<EVSubsidyApplySystem>();

            m_UnappliedStateQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadWrite<DistrictEVSubsidyState>()
            );
            RequireForUpdate(m_UnappliedStateQuery);
        }

        protected override void OnUpdate()
        {
            var entities = m_UnappliedStateQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);
                if (state.PolicyApplied) continue; // déjà testé sur ce district

                s_Log.Info($"[DEBUG HARNESS] Application automatique de test sur district {district}");

                var testConfig = new EVSubsidySliderConfig
                {
                    Uneducated = 100,
                    PoorlyEducated = 100,
                    Educated = 100,
                    WellEducated = 100,
                    HighlyEducated = 100
                };

                bool success = m_ApplySystem.TryApplySubsidyPolicy(district, testConfig);
                s_Log.Info($"[DEBUG HARNESS] Résultat application : {success}");
            }

            entities.Dispose();
        }
    }
}
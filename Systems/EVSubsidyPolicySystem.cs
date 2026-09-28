// Systems/EVSubsidyPolicySystem.cs
using Colossal.Entities;
using Colossal.Logging;
using ElectricCarSubsidy.Components;
using ElectricCarSubsidy.Utils;
using Game;
using Game.Areas;
using Game.Common;
using Game.Policies;
using Game.Simulation;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyPolicySystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private EVSubsidyPolicyRegistry m_Registry;
        private TimeSystem m_TimeSystem;
        private EntityQuery m_DistrictPolicyUpdatedQuery;

        // Systems/EVSubsidyPolicySystem.cs — version debug temporaire
        protected override void OnCreate()
        {
            base.OnCreate();

            m_Registry = World.GetOrCreateSystemManaged<EVSubsidyPolicyRegistry>();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();  // <- probablement manquante

            m_DistrictPolicyUpdatedQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
            ComponentType.ReadOnly<District>(),
            ComponentType.ReadOnly<Policy>()
                }
            });
            RequireForUpdate(m_DistrictPolicyUpdatedQuery);
        }

        protected override void OnUpdate()
        {
            Entity banPrefab = m_Registry.BanPolicyPrefabEntity;
            if (banPrefab == Entity.Null) return;

            var districts = m_DistrictPolicyUpdatedQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in districts)
            {
                bool banActive = EVSubsidyPolicyUtils.IsPolicyActive(EntityManager, district, banPrefab);
                bool hasState = EntityManager.HasComponent<DistrictEVSubsidyState>(district);

                if (banActive && !hasState)
                {
                    OnBanDetected(district);
                }
                else if (!banActive && hasState)
                {
                    OnBanRemoved(district);
                }
            }

            districts.Dispose();
        }

        private void OnBanDetected(Entity district)
        {
            s_Log.Info($"Bannissement détecté sur district {district}, initialisation EVSubsidyState");

            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);

            EntityManager.AddComponentData(district, new DistrictEVSubsidyState
            {
                SubsidizedAdultsCount = 0,
                PolicyApplied = false,
                ApplyDeadline = currentDay + 0.02f,//valeur par defaut: 1f
                SlidersLocked = false,
                LockExpiry = 0f,
                TraitActive = false,
                TraitExpiry = 0f,
                NextEligibilityCheck = 0f
            });

            EntityManager.AddBuffer<EVAdultsSnapshot>(district);
        }

        // Systems/EVSubsidyPolicySystem.cs
        private void OnBanRemoved(Entity district)
        {
            s_Log.Info($"Bannissement retiré sur district {district}");

            if (!EntityManager.HasComponent<DistrictEVSubsidyState>(district)) return;
            var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);

            if (state.TraitActive || state.DistrustTraitActive || state.AngerTraitActive)
                return;

            // On ne démonte l'état QUE si le malus de bien-être a déjà été
            // entièrement rattrapé par EVSubsidyWellbeingApplySystem. Sinon on
            // retire prématurément EVSubsidyDeficit/DistrictEVSubsidyState et le
            // système de bien-être n'a plus jamais l'occasion de corriger le delta
            // résiduel accumulé dans DistrictModifier.
            if (state.LastAppliedWellbeingDelta != 0f)
            {
                s_Log.Info($"District {district} : nettoyage différé, delta bien-être résiduel = {state.LastAppliedWellbeingDelta}");
                return;
            }

            EntityManager.RemoveComponent<DistrictEVSubsidyState>(district);
            if (EntityManager.HasComponent<EVSubsidySliderConfig>(district))
                EntityManager.RemoveComponent<EVSubsidySliderConfig>(district);
            if (EntityManager.HasComponent<EVAdoptionProgress>(district))
                EntityManager.RemoveComponent<EVAdoptionProgress>(district);
            if (EntityManager.HasBuffer<EVAdultsSnapshot>(district))
                EntityManager.RemoveComponent<EVAdultsSnapshot>(district);
            if (EntityManager.HasComponent<EVSubsidyDeficit>(district))
                EntityManager.RemoveComponent<EVSubsidyDeficit>(district);
        }
    }
}
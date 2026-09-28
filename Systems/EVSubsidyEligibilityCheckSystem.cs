// Systems/EVSubsidyEligibilityCheckSystem.cs
using Colossal;
using Colossal.Entities;
using Colossal.Logging;
using ElectricCarSubsidy.Components;
using ElectricCarSubsidy.Utils;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    [UpdateAfter(typeof(EVSubsidyPolicySystem))]
    public partial class EVSubsidyEligibilityCheckSystem : GameSystemBase
    {
        //paramètre par défaut de durée du cycle : 7f et 15f
        private const float kCycleIntervalDays = 0.1f;
        private const float kTraitDurationDays = 15f;

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EntityQuery m_ActiveStateQuery;
        private EntityQuery m_DistrictBuildingQuery;

        private float m_LastGlobalCheckDay = -1f;

        protected override void OnCreate()
        {
            base.OnCreate();

            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();

            m_ActiveStateQuery = GetEntityQuery(
                ComponentType.ReadOnly<District>(),
                ComponentType.ReadWrite<DistrictEVSubsidyState>()
            );
            RequireForUpdate(m_ActiveStateQuery);

            m_DistrictBuildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>()
            );
        }

        protected override void OnUpdate()
        {
            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);

            if (currentDay == m_LastGlobalCheckDay) return;
            m_LastGlobalCheckDay = currentDay;

            var entities = m_ActiveStateQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);
                bool dirty = false;

                // --- 1. Vérification expiration du trait (15 jours) ---
                if (state.TraitActive && currentDay >= state.TraitExpiry)
                {
                    int totalAdultsNow = EVSubsidySnapshotUtils.GetTotalAdultsFromSnapshot(EntityManager, district);
                    float ratioNow = totalAdultsNow > 0 ? (float)state.SubsidizedAdultsCount / totalAdultsNow : 0f;

                    if (ratioNow >= 0.95f)
                    {
                        // Toujours au-dessus du seuil : on reconduit pour un nouveau cycle
                        state.TraitExpiry = currentDay + kTraitDurationDays;
                        s_Log.Info($"District {district} : trait EV reconduit (ratio={ratioNow:P0}), nouvelle échéance jour {state.TraitExpiry}");
                    }
                    else
                    {
                        // Sous le seuil : le trait tombe, EVSubsidyTraitAwardSystem pourra le réoctroyer plus tard
                        state.TraitActive = false;
                        state.TraitExpiry = 0f;
                        s_Log.Info($"District {district} : trait EV arrivé à échéance et non reconduit (ratio={ratioNow:P0})");
                    }
                    dirty = true;
                }

                // --- 2. Cycle 7 jours : recalcul de la population éligible par tranche ---
                if (currentDay >= state.NextEligibilityCheck)
                {
                    int[] currentCounts = GetAdultsByEducation(district);

                    var snapshot = EntityManager.GetBuffer<EVAdultsSnapshot>(district);
                    bool anyNewEligible = false;

                    for (int tier = 0; tier < 5; tier++)
                    {
                        int previous = (tier < snapshot.Length) ? snapshot[tier].AdultCount : 0;
                        int diff = currentCounts[tier] - previous;

                        if (diff > 0)
                        {
                            anyNewEligible = true;
                            s_Log.Info($"District {district}, tranche {tier} : +{diff} adultes éligibles");
                        }
                    }

                    snapshot.Clear();
                    for (int tier = 0; tier < 5; tier++)
                    {
                        snapshot.Add(new EVAdultsSnapshot { EducationTier = (byte)tier, AdultCount = currentCounts[tier] });
                    }

                    if (anyNewEligible)
                    {
                        state.SlidersLocked = false;
                    }

                    state.NextEligibilityCheck = currentDay + kCycleIntervalDays;
                    dirty = true;
                }

                if (dirty)
                {
                    EntityManager.SetComponentData(district, state);
                }
            }

            entities.Dispose();
        }

        private int[] GetAdultsByEducation(Entity district)
        {
            var counts = new int[5];
            var buildings = m_DistrictBuildingQuery.ToEntityArray(Allocator.Temp);

            foreach (var building in buildings)
            {
                var currentDistrict = EntityManager.GetComponentData<CurrentDistrict>(building);
                if (currentDistrict.m_District != district) continue;

                if (!EntityManager.TryGetBuffer<Renter>(building, true, out var renters)) continue;

                foreach (var renter in renters)
                {
                    Entity household = renter.m_Renter;
                    if (!EntityManager.HasComponent<Household>(household)) continue;
                    if (!EntityManager.TryGetBuffer<HouseholdCitizen>(household, true, out var citizens)) continue;

                    foreach (var hc in citizens)
                    {
                        if (!EntityManager.TryGetComponent<Citizen>(hc.m_Citizen, out var citizen)) continue;
                        if (CitizenUtils.IsCorpsePickedByHearse(EntityManager, hc.m_Citizen)) continue;
                        var age = citizen.GetAge();
                        if (age != CitizenAge.Adult && age != CitizenAge.Elderly) continue;

                        int edu = citizen.GetEducationLevel();
                        if (edu >= 0 && edu < 5) counts[edu]++;
                    }
                }
            }

            buildings.Dispose();
            return counts;
        }
    }
}
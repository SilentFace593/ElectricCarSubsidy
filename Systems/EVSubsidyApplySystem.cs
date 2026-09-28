// Systems/EVSubsidyApplySystem.cs
using Colossal.Entities;
using Colossal.Logging;
using ElectricCarSubsidy.Components;
using ElectricCarSubsidy.Utils;
using Game;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ElectricCarSubsidy.Systems
{

 
    public partial class EVSubsidyApplySystem : GameSystemBase
    {
        private static readonly short[] kBasePoints = { -100, -70, -50, -30, -10 };
        private const short kPointsPerFivePercent = 10;

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EVSubsidyPolicyRegistry m_Registry;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();
            m_Registry = World.GetOrCreateSystemManaged<EVSubsidyPolicyRegistry>();
        }

        // Appelé depuis le binding UI (TriggerBinding) lors du clic "appliquer"
        public bool TryApplySubsidyPolicy(Entity district, EVSubsidySliderConfig sliderConfig)
        {
            if (!EntityManager.HasComponent<DistrictEVSubsidyState>(district))
            {
                s_Log.Warn($"TryApplySubsidyPolicy appelé sur district {district} sans state actif");
                return false;
            }

            if (!EVSubsidyPolicyUtils.IsCombustionBanActive(EntityManager, m_Registry, district))
            {
                s_Log.Info($"District {district} : bannissement inactif, application refusée");
                return false;
            }

            var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);

            if (state.SlidersLocked)
            {
                s_Log.Info($"District {district} : sliders verrouillés, application refusée");
                return false;
            }

            float currentDay = GameTimeUtils.GetCurrentGameDay(m_TimeSystem);

            // --- 1. Calcul du coût total ---
            int[] eligibleByTier = GetEligibleCountByTier(district);
            float vehicleCost = GetVehicleCostSetting();

            byte[] subsidyPct = new byte[]
            {
                sliderConfig.Uneducated, sliderConfig.PoorlyEducated,
                sliderConfig.Educated, sliderConfig.WellEducated, sliderConfig.HighlyEducated
            };

            int totalCost = 0;
            for (int tier = 0; tier < 5; tier++)
            {
                float subsidyAmount = vehicleCost * (subsidyPct[tier] / 100f);
                totalCost += (int)(subsidyAmount * eligibleByTier[tier]);
            }

            // --- Calcul de la subvention moyenne pondérée (pour lever DistrustTraitActive) ---
            int totalEligible = 0;
            float weightedSubsidySum = 0f;
            for (int tier = 0; tier < 5; tier++)
            {
                totalEligible += eligibleByTier[tier];
                weightedSubsidySum += subsidyPct[tier] * eligibleByTier[tier];
            }
            float weightedAverageSubsidy = totalEligible > 0 ? weightedSubsidySum / totalEligible : 0f;

            bool anySubsidy = false;
            for (int tier = 0; tier < 5; tier++)
            {
                if (subsidyPct[tier] > 0) { anySubsidy = true; break; }
            }
            if (!anySubsidy)
            {
                s_Log.Info($"District {district} : aucune subvention configurée (0% partout), application refusée");
                return false;
            }

            // --- 2. Vérification budgétaire via le singleton PlayerMoney ---
            if (!SystemAPI.HasSingleton<PlayerMoney>())
            {
                s_Log.Warn("PlayerMoney singleton introuvable, application refusée");
                return false;
            }

            var playerMoney = SystemAPI.GetSingletonRW<PlayerMoney>();
            if (playerMoney.ValueRO.money < totalCost)
            {
                s_Log.Info($"District {district} : coût {totalCost} > fonds disponibles {playerMoney.ValueRO.money}, application refusée");
                return false;
            }

            // --- 3. Calcul des points compensés et du déficit résiduel ---
            short[] deficitValues = new short[5];
            for (int tier = 0; tier < 5; tier++)
            {
                short compensated = (short)((subsidyPct[tier] / 5) * kPointsPerFivePercent);
                deficitValues[tier] = (short)math.min(0, kBasePoints[tier] + compensated);
            }

            var deficit = new EVSubsidyDeficit
            {
                Uneducated = deficitValues[0],
                PoorlyEducated = deficitValues[1],
                Educated = deficitValues[2],
                WellEducated = deficitValues[3],
                HighlyEducated = deficitValues[4]
            };

            // --- 4. Débit des finances de la ville ---
            playerMoney.ValueRW.Subtract(totalCost);

            // --- 5. Application : init des components requis par EVAdoptionProgressSystem ---
            if (!EntityManager.HasComponent<EVSubsidySliderConfig>(district))
                EntityManager.AddComponentData(district, sliderConfig);
            else
                EntityManager.SetComponentData(district, sliderConfig);

            if (!EntityManager.HasComponent<EVAdoptionProgress>(district))
                EntityManager.AddComponentData(district, new EVAdoptionProgress());

            if (!EntityManager.HasComponent<EVAdoptionTimer>(district))
                EntityManager.AddComponentData(district, new EVAdoptionTimer());

            if (!EntityManager.HasComponent<EVSubsidyDeficit>(district))
                EntityManager.AddComponentData(district, deficit);
            else
                EntityManager.SetComponentData(district, deficit);

            state.CarriedOverRecipients = CountAlreadySubsidized(district);
            state.PolicyApplied = true;
            state.NextEligibilityCheck = currentDay + 7f;
            if (state.DistrustTraitActive && weightedAverageSubsidy >= 50f)
            {
                s_Log.Info($"District {district} : trait négatif levé (subvention moyenne pondérée {weightedAverageSubsidy:F1}% >= 50%)");
                state.DistrustTraitActive = false;
            }

            EntityManager.SetComponentData(district, state);

            s_Log.Info($"District {district} : politique appliquée, coût={totalCost}, fonds restants={playerMoney.ValueRO.money}");
            return true;
        }

        private int CountAlreadySubsidized(Entity district)
        {
            int count = 0;
            var buildingQuery = GetEntityQuery(
                ComponentType.ReadOnly<Building>(),
                ComponentType.ReadOnly<ResidentialProperty>(),
                ComponentType.ReadOnly<PrefabRef>(),
                ComponentType.ReadOnly<Renter>(),
                ComponentType.ReadOnly<CurrentDistrict>(),
                ComponentType.Exclude<Temp>(),
                ComponentType.Exclude<Deleted>()
            );
            var buildings = buildingQuery.ToEntityArray(Allocator.Temp);

            foreach (var building in buildings)
            {
                var currentDistrict = EntityManager.GetComponentData<CurrentDistrict>(building);
                if (currentDistrict.m_District != district) continue;
                if (!EntityManager.TryGetBuffer<Renter>(building, true, out var renters)) continue;

                foreach (var renter in renters)
                {
                    if (!EntityManager.HasComponent<Household>(renter.m_Renter)) continue;
                    if (!EntityManager.TryGetBuffer<HouseholdCitizen>(renter.m_Renter, true, out var citizens)) continue;

                    foreach (var hc in citizens)
                    {
                        if (EntityManager.HasComponent<EVSubsidyRecipient>(hc.m_Citizen))
                            count++;
                    }
                }
            }

            buildings.Dispose();
            return count;
        }

        private int[] GetEligibleCountByTier(Entity district)
        {
            var counts = new int[5];
            if (!EntityManager.HasBuffer<EVAdultsSnapshot>(district)) return counts;

            var snapshot = EntityManager.GetBuffer<EVAdultsSnapshot>(district);
            for (int i = 0; i < snapshot.Length; i++)
            {
                if (snapshot[i].EducationTier < 5)
                    counts[snapshot[i].EducationTier] = snapshot[i].AdultCount;
            }
            return counts;
        }

        private float GetVehicleCostSetting()
        {
            return Mod.Instance.Settings.VehicleCost;
        }

        protected override void OnUpdate() { }
    }
}
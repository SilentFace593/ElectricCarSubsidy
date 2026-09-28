// Systems/EVAdoptionProgressSystem.cs
using System.Collections.Generic;
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
using Unity.Mathematics;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVAdoptionProgressSystem : GameSystemBase
    {
        //paramètre de durée en jours  : valeur par defaut 7f pour 7 jours
        private const float kBaseDurationDays = 0.1f;
        private const float kMinutesPerDay = 1440f;

        // Facteur de vitesse par tranche d'éducation (index 0 = Sans Instruction)
        private static readonly float[] kEducationSpeedFactor = { 1.3f, 1.15f, 1.0f, 0.85f, 0.7f };

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private TimeSystem m_TimeSystem;
        private EntityQuery m_ActiveStateQuery;
        private EntityQuery m_DistrictBuildingQuery;

        private float m_LastCheckMinute = -1f;

        // EVAdoptionProgressSystem.cs — nouvelle méthode
        private float GetSpeedBonusMultiplier(byte subsidyPct)
        {
            float bonus = 0f;
            if (subsidyPct >= 50) bonus += 0.15f;
            if (subsidyPct >= 75) bonus += 0.25f;
            if (subsidyPct >= 100) bonus += 0.35f;
            return 1f + bonus; // 1.0 à 1.45 selon le palier
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_TimeSystem = World.GetOrCreateSystemManaged<TimeSystem>();

            m_ActiveStateQuery = GetEntityQuery(
    ComponentType.ReadOnly<District>(),
    ComponentType.ReadWrite<DistrictEVSubsidyState>(),
    ComponentType.ReadWrite<EVAdoptionProgress>(),
    ComponentType.ReadWrite<EVAdoptionTimer>(),
    ComponentType.ReadOnly<EVSubsidySliderConfig>()
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
            float currentMinute = math.floor(currentDay * kMinutesPerDay);

            // Throttle : 1x par minute de jeu, pas plus
            if (currentMinute == m_LastCheckMinute) return;
            m_LastCheckMinute = currentMinute;

            var entities = m_ActiveStateQuery.ToEntityArray(Allocator.Temp);

            foreach (var district in entities)
            {
                var state = EntityManager.GetComponentData<DistrictEVSubsidyState>(district);
                if (!state.PolicyApplied) continue; // pas encore validé par le joueur

                var progress = EntityManager.GetComponentData<EVAdoptionProgress>(district);
                var timer = EntityManager.HasComponent<EVAdoptionTimer>(district)
                    ? EntityManager.GetComponentData<EVAdoptionTimer>(district)
                    : default;

                float[] timerValues = new float[]
                {
    timer.Uneducated, timer.PoorlyEducated, timer.Educated, timer.WellEducated, timer.HighlyEducated
                };
                var sliderConfig = EntityManager.GetComponentData<EVSubsidySliderConfig>(district);

                byte[] subsidyPct = new byte[]
                {
                    sliderConfig.Uneducated,
                    sliderConfig.PoorlyEducated,
                    sliderConfig.Educated,
                    sliderConfig.WellEducated,
                    sliderConfig.HighlyEducated
                };

                float[] progressValues = new float[]
{
    progress.Uneducated,
    progress.PoorlyEducated,
    progress.Educated,      // corrigé ici
    progress.WellEducated,
    progress.HighlyEducated
};

                // Récupère les citoyens éligibles par tranche (avec entités, pas juste comptage)
                List<Entity>[] eligibleByTier = GetAdultsByEducationDetailed(district);

                bool progressDirty = false;

                for (int tier = 0; tier < 5; tier++)
                {
                    int diagCurrent = CountRecipients(eligibleByTier[tier]);
                    s_Log.Info($"[TIER DIAG] District {district} tranche {tier} : subsidyPct={subsidyPct[tier]}, timerValue={timerValues[tier]:F3}, currentCount={diagCurrent}, eligibleCount={eligibleByTier[tier].Count}");

                    if (subsidyPct[tier] <= 0) continue;

                    int eligibleCount = eligibleByTier[tier].Count;
                    int currentCount = CountRecipients(eligibleByTier[tier]);

                    // Le rattrapage continue même une fois le minuteur saturé à 1.0 — le
                    // minuteur ne fait qu'accélérer/ralentir le RYTHME d'assignation, il ne
                    // doit jamais bloquer le fait d'atteindre 100% une fois le temps écoulé.
                    if (currentCount >= eligibleCount)
                    {
                        timerValues[tier] = math.min(1f, timerValues[tier]); // rien à faire, déjà complet
                        continue;
                    }

                    float speedBonus = GetSpeedBonusMultiplier(subsidyPct[tier]);
                    float durationDays = kBaseDurationDays * kEducationSpeedFactor[tier]
                        / math.max(0.1f, subsidyPct[tier] / 50f)
                        / speedBonus;
                    float progressPerMinute = 1f / (durationDays * kMinutesPerDay);

                    timerValues[tier] = math.min(1f, timerValues[tier] + progressPerMinute);

                    int rawTarget = timerValues[tier] >= 1f
                        ? eligibleCount // le minuteur est plein : la cible devient tout l'effectif
                        : (int)math.floor(timerValues[tier] * eligibleCount);

                    int scaledCap = (int)math.ceil(progressPerMinute * eligibleCount * 5f);
                    int expectedThisTick = math.max(3, scaledCap);
                    int delta = math.min(rawTarget - currentCount, expectedThisTick);

                    if (delta > 0)
                    {
                        AssignSubsidyToNewCitizens(eligibleByTier[tier], delta, tier, district, ref state);
                        currentCount += delta;
                    }

                    progressValues[tier] = eligibleCount > 0 ? (float)currentCount / eligibleCount : 0f;
                    progressDirty = true;
                }



                int totalRecipients = 0;
                for (int tier = 0; tier < 5; tier++)
                {
                    totalRecipients += CountRecipients(eligibleByTier[tier]);
                }
                state.SubsidizedAdultsCount = totalRecipients;

                if (progressDirty)
                {
                    progress.Uneducated = progressValues[0];
                    progress.PoorlyEducated = progressValues[1];
                    progress.Educated = progressValues[2];
                    progress.WellEducated = progressValues[3];
                    progress.HighlyEducated = progressValues[4];

                    timer.Uneducated = timerValues[0];
                    timer.PoorlyEducated = timerValues[1];
                    timer.Educated = timerValues[2];
                    timer.WellEducated = timerValues[3];
                    timer.HighlyEducated = timerValues[4];

                    EntityManager.SetComponentData(district, progress);
                    if (EntityManager.HasComponent<EVAdoptionTimer>(district))
                        EntityManager.SetComponentData(district, timer);
                    else
                        EntityManager.AddComponentData(district, timer);
                }

                EntityManager.SetComponentData(district, state); // toujours, même si progressDirty est faux
            }

            entities.Dispose();
        }

        private int CountRecipients(List<Entity> citizens)
        {
            int count = 0;
            foreach (var c in citizens)
            {
                if (EntityManager.HasComponent<EVSubsidyRecipient>(c)) count++;
            }
            return count;
        }

        private void AssignSubsidyToNewCitizens(List<Entity> tierCitizens, int count, int tier, Entity district, ref DistrictEVSubsidyState state)
        {
            int assigned = 0;
            foreach (var citizen in tierCitizens)
            {
                if (assigned >= count) break;
                if (EntityManager.HasComponent<EVSubsidyRecipient>(citizen)) continue;

                EntityManager.AddComponentData(citizen, new EVSubsidyRecipient
                {
                    EducationTierAtSubsidy = (byte)tier
                });

                assigned++;
            }

            state.SubsidizedAdultsCount += assigned;

            if (assigned > 0)
            {
                s_Log.Info($"District {district}, tranche {tier} : +{assigned} nouveaux citoyens subventionnés");
            }
        }

        // Variante de GetAdultsByEducation qui retourne les entités, pas juste un compte
        private List<Entity>[] GetAdultsByEducationDetailed(Entity district)
        {
            var result = new List<Entity>[5];
            for (int i = 0; i < 5; i++) result[i] = new List<Entity>();

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
                        if (edu >= 0 && edu < 5) result[edu].Add(hc.m_Citizen);
                    }
                }
            }

            buildings.Dispose();
            return result;
        }
    }
}
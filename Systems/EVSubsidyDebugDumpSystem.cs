// Systems/EVSubsidyDebugDumpSystem.cs
using Colossal.Logging;
using Game;
using Game.Areas;
using Game.Prefabs;
using Game.SceneFlow;
using Unity.Entities;
using Unity.Collections;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyDebugDumpSystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private PrefabSystem m_PrefabSystem;
        private EntityQuery m_DistrictPolicyPrefabQuery;
        private bool m_DumpDone;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();

            m_DistrictPolicyPrefabQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<PolicyData>() },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<DistrictOptionData>(),
                    ComponentType.ReadOnly<DistrictModifierData>()
                }
            });
        }

        protected override void OnUpdate()
        {
            if (m_DumpDone) return;
            if (m_DistrictPolicyPrefabQuery.IsEmptyIgnoreFilter) return;

            var entities = m_DistrictPolicyPrefabQuery.ToEntityArray(Allocator.Temp);
            s_Log.Info($"=== Dump des PolicyPrefab de district ({entities.Length}) ===");

            foreach (var e in entities)
            {
                var prefab = m_PrefabSystem.GetPrefab<PolicyPrefab>(e);
                string techName = prefab.name;

                string localizedTitle = "(non trouvé)";
                if (GameManager.instance.localizationManager.activeDictionary
                    .TryGetValue($"Policy.TITLE[{techName}]", out var title))
                {
                    localizedTitle = title;
                }

                bool hasOption = EntityManager.HasComponent<DistrictOptionData>(e);
                bool hasModifier = EntityManager.HasComponent<DistrictModifierData>(e);
                bool hasSlider = EntityManager.HasComponent<PolicySliderData>(e);

                s_Log.Info($"[{techName}] title=\"{localizedTitle}\" option={hasOption} modifier={hasModifier} slider={hasSlider}");
            }

            entities.Dispose();
            m_DumpDone = true;
        }
    }
}
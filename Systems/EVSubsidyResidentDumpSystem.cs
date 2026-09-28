// Systems/EVSubsidyResidentDumpSystem.cs
using Colossal.Logging;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyResidentDumpSystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private EntityQuery m_DistrictQuery;
        private bool m_DumpDone;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
        }

        protected override void OnUpdate()
        {
            if (m_DumpDone) return;
            if (m_DistrictQuery.IsEmptyIgnoreFilter) return;

            var entities = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0) { entities.Dispose(); return; }

            // On inspecte uniquement le premier district trouvé, histoire de
            // lister tous les component types présents sur son entité
            Entity sample = entities[0];
            s_Log.Info($"=== Dump components sur District {sample} ===");

            var chunk = EntityManager.GetChunk(sample);
            var types = chunk.Archetype.GetComponentTypes();

            foreach (var type in types)
            {
                s_Log.Info($"  - {type}");
            }

            entities.Dispose();
            m_DumpDone = true;
        }
    }
}
// Systems/EVSubsidyWellbeingDumpSystem.cs
using Colossal.Logging;
using Game;
using Game.Areas;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    public partial class EVSubsidyWellbeingDumpSystem : GameSystemBase
    {
        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private EntityQuery m_DistrictQuery;
        private bool m_DumpDone;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_DistrictQuery = GetEntityQuery(ComponentType.ReadOnly<District>());
        }


        // Systems/EVSubsidyWellbeingDumpSystem.cs — méthode OnUpdate corrigée
        protected override void OnUpdate()
        {
            if (m_DumpDone) return;
            if (m_DistrictQuery.IsEmptyIgnoreFilter) return;

            var entities = m_DistrictQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length == 0) { entities.Dispose(); return; }

            Entity sample = entities[0];
            s_Log.Info($"=== Dump complet components sur District {sample} ===");

            var chunk = EntityManager.GetChunk(sample);
            var types = chunk.Archetype.GetComponentTypes();

            foreach (var type in types)
            {
                var managedType = type.GetManagedType();
                string typeName = managedType != null ? managedType.FullName : $"(non managé: {type})";
                s_Log.Info($"  - {typeName}");
            }
            DumpDistrictModifierFields();


            entities.Dispose();
            m_DumpDone = true;
        }
        // Ajoute temporairement cette méthode à EVSubsidyWellbeingDumpSystem, appelée juste après le dump des types
        private void DumpDistrictModifierFields()
        {
            var type = typeof(Game.Areas.DistrictModifier);
            s_Log.Info($"=== Champs de {type.FullName} ===");

            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                s_Log.Info($"  {field.FieldType.Name} {field.Name}");
            }
        }
    }
    }

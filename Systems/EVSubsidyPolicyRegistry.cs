using Colossal.Logging;
using Colossal.Serialization.Entities;
using Game;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace ElectricCarSubsidy.Systems
{
    // Système "service" : ne fait rien à chaque frame, sert uniquement
    // de point d'accès centralisé à l'entité prefab du bannissement,
    // résolue une seule fois au chargement de partie.
    public partial class EVSubsidyPolicyRegistry : GameSystemBase
    {
        private const string kBanPolicyName = "Combustion Engine Ban";

        private static readonly ILog s_Log = LogManager.GetLogger("ElectricCarSubsidy");

        private PrefabSystem m_PrefabSystem;

        public Entity BanPolicyPrefabEntity { get; private set; } = Entity.Null;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
        }

        protected override void OnGameLoaded(Context serializationContext)
        {
            base.OnGameLoaded(serializationContext);
            ResolveBanPolicyPrefab();
        }

        private void ResolveBanPolicyPrefab()
        {
            var query = GetEntityQuery(ComponentType.ReadOnly<PolicyData>());
            var entities = query.ToEntityArray(Allocator.Temp);

            BanPolicyPrefabEntity = Entity.Null;

            foreach (var e in entities)
            {
                var prefab = m_PrefabSystem.GetPrefab<PolicyPrefab>(e);
                if (prefab.name == kBanPolicyName)
                {
                    BanPolicyPrefabEntity = e;
                    s_Log.Info($"[Registry] Combustion Engine Ban prefab résolu : {e}");
                    break;
                }
            }

            entities.Dispose();

            if (BanPolicyPrefabEntity == Entity.Null)
            {
                s_Log.Warn("[Registry] Impossible de résoudre le prefab Combustion Engine Ban !");
            }
        }

        // Pas de logique per-frame nécessaire, ce système est purement passif
        protected override void OnUpdate() { }
    }
}
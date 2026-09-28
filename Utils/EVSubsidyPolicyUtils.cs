// Utils/EVSubsidyPolicyUtils.cs — méthode ajoutée
using Colossal.Entities;
using ElectricCarSubsidy.Systems;
using Game.Areas;
using Game.Policies;
using Unity.Entities;

namespace ElectricCarSubsidy.Utils
{
    public static class EVSubsidyPolicyUtils
    {
        public static bool IsPolicyActive(EntityManager em, Entity target, Entity policyPrefabEntity)
        {
            if (!em.TryGetBuffer<Policy>(target, true, out var buffer))
                return false;

            for (int i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].m_Policy == policyPrefabEntity)
                {
                    return (buffer[i].m_Flags & PolicyFlags.Active) != 0;
                }
            }
            return false;
        }

        // Factorisation : vérifie si le bannissement des moteurs thermiques est actif
        // sur un district donné, via le prefab résolu par le registre central.
        public static bool IsCombustionBanActive(EntityManager em, EVSubsidyPolicyRegistry registry, Entity district)
        {
            Entity banPrefab = registry.BanPolicyPrefabEntity;
            if (banPrefab == Entity.Null) return false;
            return IsPolicyActive(em, district, banPrefab);
        }
    }
}
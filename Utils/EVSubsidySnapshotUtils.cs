// Utils/EVSubsidySnapshotUtils.cs
using Colossal.Entities;
using ElectricCarSubsidy.Components;
using Unity.Entities;

namespace ElectricCarSubsidy.Utils
{
    public static class EVSubsidySnapshotUtils
    {
        public static int GetTotalAdultsFromSnapshot(EntityManager em, Entity district)
        {
            if (!em.HasBuffer<EVAdultsSnapshot>(district)) return 0;

            var snapshot = em.GetBuffer<EVAdultsSnapshot>(district);
            int total = 0;
            for (int i = 0; i < snapshot.Length; i++) total += snapshot[i].AdultCount;
            return total;
        }
    }
}
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace ElectricCarSubsidy.Components
{
    public struct DistrictEVSubsidyState : IComponentData, ISerializable
    {
        public int SubsidizedAdultsCount;
        public bool PolicyApplied;
        public float ApplyDeadline;
        public bool SlidersLocked;
        public float LockExpiry;
        public bool TraitActive;
        public float TraitExpiry;
        public float NextEligibilityCheck;
        public bool DistrustTraitActive;
        public float DistrustTraitExpiry;
        public bool DeadlinePenaltyApplied;
        public float LastAppliedWellbeingDelta;
        public bool AngerTraitActive;
        public int CarriedOverRecipients;

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(SubsidizedAdultsCount);
            writer.Write(PolicyApplied);
            writer.Write(ApplyDeadline);
            writer.Write(SlidersLocked);
            writer.Write(LockExpiry);
            writer.Write(TraitActive);
            writer.Write(TraitExpiry);
            writer.Write(NextEligibilityCheck);
            writer.Write(DistrustTraitActive);
            writer.Write(DistrustTraitExpiry);
            writer.Write(DeadlinePenaltyApplied);
            writer.Write(LastAppliedWellbeingDelta);
            writer.Write(AngerTraitActive);
            writer.Write(CarriedOverRecipients);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out SubsidizedAdultsCount);
            reader.Read(out PolicyApplied);
            reader.Read(out ApplyDeadline);
            reader.Read(out SlidersLocked);
            reader.Read(out LockExpiry);
            reader.Read(out TraitActive);
            reader.Read(out TraitExpiry);
            reader.Read(out NextEligibilityCheck);
            reader.Read(out DistrustTraitActive);
            reader.Read(out DistrustTraitExpiry);
            reader.Read(out DeadlinePenaltyApplied);
            reader.Read(out LastAppliedWellbeingDelta);
            reader.Read(out AngerTraitActive);
            reader.Read(out CarriedOverRecipients);
        }

      
    }
}
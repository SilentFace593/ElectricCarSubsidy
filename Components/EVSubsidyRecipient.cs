// Components/EVSubsidyRecipient.cs
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    // Tag component posé sur l'entité Citizen une fois qu'elle a "reçu" sa subvention
    // (adoption terminée, pas juste éligible)
    public struct EVSubsidyRecipient : IComponentData, ISerializable
    {
        public byte EducationTierAtSubsidy; // pour audit / debug, pas strictement nécessaire

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(EducationTierAtSubsidy);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out EducationTierAtSubsidy);
        }



    }
}
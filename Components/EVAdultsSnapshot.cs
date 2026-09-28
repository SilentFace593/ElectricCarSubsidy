using Unity.Entities;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    public struct EVAdultsSnapshot : IBufferElementData, ISerializable
    {
        public byte EducationTier;
        public int AdultCount;
   

    public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(EducationTier);
            writer.Write(AdultCount);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out EducationTier);
            reader.Read(out AdultCount);
        }

    }
}
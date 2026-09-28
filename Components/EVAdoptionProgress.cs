// Components/EVAdoptionProgress.cs
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    // Buffer/component sur District : progression en cours par tranche (0.0 à 1.0)
    public struct EVAdoptionProgress : IComponentData, ISerializable
    {
        public float Uneducated;
        public float PoorlyEducated;
        public float Educated;
        public float WellEducated;
        public float HighlyEducated;
   

     public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            writer.Write(Uneducated);
            writer.Write(PoorlyEducated);
            writer.Write(Educated);
            writer.Write(WellEducated);
            writer.Write(HighlyEducated);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out Uneducated);
            reader.Read(out PoorlyEducated);
            reader.Read(out Educated);
            reader.Read(out WellEducated);
            reader.Read(out HighlyEducated);
        }

    }
}

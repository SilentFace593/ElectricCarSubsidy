using Unity.Entities;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    // Minuteur interne par tranche, purement monotone — jamais synchronisé
    // avec le ratio réel affiché à l'UI (EVAdoptionProgress), pour ne jamais
    // perdre l'accumulation progressive entre deux ticks.
    public struct EVAdoptionTimer : IComponentData, ISerializable
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
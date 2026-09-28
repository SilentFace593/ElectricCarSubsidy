// Components/EVSubsidyDeficit.cs
using Unity.Entities;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    // Stocke le déficit de points restant par tranche, pour alimenter
    // le modifier de mécontentement de façon continue
    public struct EVSubsidyDeficit : IComponentData, ISerializable
    {
        public short Uneducated;      // points négatifs restants (0 si compensé)
        public short PoorlyEducated;
        public short Educated;
        public short WellEducated;
        public short HighlyEducated;
    

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
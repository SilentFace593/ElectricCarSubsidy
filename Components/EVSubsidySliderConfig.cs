// Components/EVSubsidySliderConfig.cs
using Unity.Entities;
using Unity.Mathematics;
using Colossal.Serialization.Entities;

namespace ElectricCarSubsidy.Components
{
    // Un component sur l'entité District, stocke le % de subvention par tranche d'éducation
    public struct EVSubsidySliderConfig : IComponentData, ISerializable

    {
        public byte Uneducated;      // 0-100, par palier de 5
        public byte PoorlyEducated;
        public byte Educated;
        public byte WellEducated;
        public byte HighlyEducated;

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
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(List<Sku>))]
[JsonSerializable(typeof(Sku))]
[JsonSerializable(typeof(Capability))]
[JsonSerializable(typeof(LocationInfo))]
[JsonSerializable(typeof(ZoneDetail))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

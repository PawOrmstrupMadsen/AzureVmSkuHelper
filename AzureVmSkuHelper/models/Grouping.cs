public class Grouping
{
	public required string Family { get; set; }
	public required string CpuType { get; set; }
	public required string Version { get; set; }
	public IEnumerable<Sku> Skus { get; set; } = [];

}
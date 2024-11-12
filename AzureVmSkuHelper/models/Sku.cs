public class Sku
{
	public string? ApiVersions { get; set; }
	public Capability[] Capabilities { get; set; } = [];
	public long? Capacity { get; set; }
	public string? Costs { get; set; }
	public string? Family { get; set; }
	public string? Kind { get; set; }
	public LocationInfo[] LocationInfo { get; set; } = [];
	public string[] Locations { get; set; } = [];
	public string? Name { get; set; }
	public string? ResourceType { get; set; }
	public string[] Restrictions { get; set; } = [];
	public string? Size { get; set; }
	public string? Tier { get; set; }
}

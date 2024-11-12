using CommandLine;

public class Options
{
	[Option("SubscriptionId", Required = true)]
	public required string SubscriptionId { get; set; }

	[Option("Location", Default = "norwayeast")]
	public required string Location { get; set; }

	[Option("CoreCount", Default = 4)]
	public int CoreCount { get; set; }

	[Option("Memory", Default = 8)]
	public int Memory { get; set; }

	[Option("PreferredFamily", Default = Family.B)]
	public Family PreferredFamily { get; set; }

	[Option("PreferredCpuType", Default = CpuType.AMD)]
	public CpuType PreferredCpuType { get; set; }

}

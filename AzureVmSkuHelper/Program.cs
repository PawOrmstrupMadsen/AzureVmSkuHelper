using Azure.ResourceManager;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Compute;
using System.Text.Json;
using CommandLine;

var optionResult = Parser.Default.ParseArguments<Options>(args);
var result = await optionResult.WithParsedAsync(async options =>
{
	var skus = new List<Sku>();
	if (File.Exists("skus.json"))
	{
		var fileContent = await File.ReadAllTextAsync("skus.json");
		skus = JsonSerializer.Deserialize<List<Sku>>(json: fileContent, jsonTypeInfo: SourceGenerationContext.Default.ListSku);
	}
	else
	{
		TokenCredential cred = new DefaultAzureCredential();
		// authenticate your client
		ArmClient client = new ArmClient(cred);
		ResourceIdentifier subscriptionResourceId = SubscriptionResource.CreateResourceIdentifier(options.SubscriptionId);
		SubscriptionResource subscriptionResource = client.GetSubscriptionResource(subscriptionResourceId);
		skus = subscriptionResource.GetComputeResourceSkusAsync().ToBlockingEnumerable().Select(x => new Sku()
		{
			Capabilities = x.Capabilities.Select(y => new Capability()
			{
				Name = y.Name,
				Value = y.Value
			}).ToArray(),
			Family = x.Family,
			Kind = x.Kind,
			LocationInfo = x.LocationInfo.Select(y => new LocationInfo()
			{
				Location = y.Location?.ToString(),
				ZoneDetails = y.ZoneDetails.Select(z => new ZoneDetail()
				{
					Name = z.Name,
					Capabilities = z.Capabilities.Select(a => new Capability()
					{
						Name = a.Name,
						Value = a.Value
					}).ToArray()
				}).ToArray(),
			}).ToArray(),
			Locations = x.Locations.Select(y => y.Name).ToArray(),
			Name = x.Name,
			ResourceType = x.ResourceType,
			Size = x.Size,
			Tier = x.Tier
		}).ToList();
		await File.WriteAllTextAsync("skus.json", JsonSerializer.Serialize(skus, jsonTypeInfo: SourceGenerationContext.Default.ListSku));
	}
	if (skus == null)
	{
		Console.WriteLine("Fetching SKU list went bad :(");
		return;
	}
	var list = skus
		.Where(x => x.ResourceType == "virtualMachines")
		.Where(x => x.Capabilities.Any(y => y.Name == "vCPUs" && y.Value == options.CoreCount.ToString()))
		.Where(x => x.Capabilities.Any(y => y.Name == "PremiumIO" && y.Value == "True"))
		.Where(x => x.Capabilities.Any(y => y.Name == "MemoryGB" && y.Value == options.Memory.ToString()))
		.Where(x => x.LocationInfo.Where(x => x.Location != null).Any(x => x.Location! == new AzureLocation(options.Location)))
		.ToList();

	var list2 = new List<DeconstructedSkuModel>();

	foreach (var sku in list)
	{
		var skuSizeParts = sku.Size?.Split("_");
		if (skuSizeParts?.Length != 2)
		{
			continue;
		}
		var rawFamily = skuSizeParts[0];
		var rawGeneration = skuSizeParts[1];

		int coreCountIndex = rawFamily.IndexOfAny("0123456789".ToCharArray());
		var family = rawFamily.Substring(0, coreCountIndex);
		if (family == "DC")
		{
			continue;
		}
		var arm64 = rawFamily.Substring(coreCountIndex + 1).StartsWith("p");
		if (arm64)
		{
			continue;
		}
		var tempDisk = rawFamily.Substring(coreCountIndex + 1).Contains("d");
		if (tempDisk)
		{
			continue;
		}
		var amd = rawFamily.Substring(coreCountIndex + 1).StartsWith("a");
		var premiumStorage = rawFamily.Substring(coreCountIndex + 1).Contains("s");
		var lowMem = rawFamily.Substring(coreCountIndex + 1).Contains("l");
		var highMem = rawFamily.Substring(coreCountIndex + 1).Contains("m");
		var tinyMem = rawFamily.Substring(coreCountIndex + 1).Contains("t");

		var version = rawGeneration.Substring(1);
		list2.Add(new DeconstructedSkuModel()
		{
			CpuType = amd ? "amd" : "intel",
			Family = family,
			Sku = sku,
			Version = version
		});
	}
	var grouped = list2.GroupBy(x => new { x.Family, x.CpuType, x.Version }).Select(x => new Grouping
	{
		Family = x.Key.Family,
		CpuType = x.Key.CpuType,
		Version = x.Key.Version,
		Skus = x.Select(y => y.Sku)
	}).ToList();
	var families = grouped.Select(x => x.Family).Distinct().ToList();

	var latestVersionInEachFamily = new Dictionary<string, string>();
	foreach (var family in families)
	{
		latestVersionInEachFamily.Add(family, grouped.Where(x => x.Family == family).OrderByDescending(x => x.Version).FirstOrDefault()?.Version ?? "-1");
	}
	var result = new List<Grouping>();
	foreach (var kv in latestVersionInEachFamily)
	{
		result.AddRange(grouped.Where(x => x.Family == kv.Key && x.Version == kv.Value).ToList());
	}
	var recommendedSku = result.OrderBy(x => x.Family.Equals(options.PreferredFamily.ToString(), StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
		.ThenBy(x => x.CpuType.Equals(options.PreferredCpuType.ToString(), StringComparison.InvariantCultureIgnoreCase) ? 0 : 1)
		.ThenBy(x => x.Version)
		.FirstOrDefault();
	if (recommendedSku?.Skus.FirstOrDefault() == null)
	{
		Console.WriteLine("No recommended SKU found");
		return;
	}
	Console.WriteLine(recommendedSku?.Skus.FirstOrDefault()?.Name);
	return;

});
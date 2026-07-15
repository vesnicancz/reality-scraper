namespace RealityScraper.Web.Shared.Models.ScraperTasks;

public static class ScraperTypeDisplay
{
	private static readonly IReadOnlyDictionary<ScraperType, string> Names = new Dictionary<ScraperType, string>
	{
		[ScraperType.SReality] = "SReality",
		[ScraperType.RealityIdnes] = "Reality Idnes"
	};

	public static string GetName(ScraperType scraperType) =>
		Names.TryGetValue(scraperType, out var name) ? name : scraperType.ToString();
}

using RealityScraper.Domain.Enums;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Application.Tests.Features.ScraperTasks;

/// <summary>
/// Doménový <see cref="ScrapersEnum"/> a klientský <see cref="ScraperType"/> (Web.Shared)
/// jsou propojené syrovým (int) castem na hranici API. Klient nesmí referencovat Domain,
/// takže enum existuje dvakrát - tento test hlídá, aby oba nezdriftovaly.
/// </summary>
public class ScraperEnumParityTests
{
	[Fact]
	public void ScrapersEnum_And_ScraperType_HaveIdenticalMembers()
	{
		var domain = Enum.GetValues<ScrapersEnum>()
			.ToDictionary(v => v.ToString(), v => (int)v);
		var shared = Enum.GetValues<ScraperType>()
			.ToDictionary(v => v.ToString(), v => (int)v);

		Assert.Equal(domain, shared);
	}
}

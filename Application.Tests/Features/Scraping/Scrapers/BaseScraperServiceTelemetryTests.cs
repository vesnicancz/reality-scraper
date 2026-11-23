using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Moq;
using RealityScraper.Application.Configuration;
using RealityScraper.Application.Diagnostics;
using RealityScraper.Application.Features.Scraping.Configuration;
using RealityScraper.Application.Features.Scraping.Scrapers;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.Domain.Enums;

namespace RealityScraper.Application.Tests.Features.Scraping.Scrapers;

/// <summary>
/// Metriky a spany scrapování. Instrumenty jsou statické a testy běží paralelně, proto
/// každý test filtruje měření podle vlastního (unikátního) názvu portálu.
/// </summary>
public class BaseScraperServiceTelemetryTests
{
	private const string ValidUrl = "https://www.sreality.cz/hledani/prodej/domy";

	[Fact]
	public async Task ScrapeListingsAsync_SinglePage_RecordsListingCountsPagesAndDuration()
	{
		// arrange - jedna platná karta a jedna bez odkazu (reklama -> skipped), žádná další stránka
		var siteName = $"telemetry-{Guid.NewGuid():N}";
		var driver = new Mock<IWebDriver>();
		driver.Setup(x => x.FindElementsAsync("li", It.IsAny<CancellationToken>()))
			.ReturnsAsync(new[] { CreateListingCard("https://www.sreality.cz/detail/prodej/dum/123"), CreateListingCard(null) });
		driver.Setup(x => x.FindElementsAsync(".next", It.IsAny<CancellationToken>()))
			.ReturnsAsync(Array.Empty<IWebDriverElement>());

		using var measurements = new MeasurementRecorder(siteName);
		using var activities = new ActivityRecorder(siteName);
		var sut = CreateSut(siteName, driver.Object);

		// act
		var result = await sut.ScrapeListingsAsync(new ScraperConfiguration { Url = ValidUrl }, CancellationToken.None);

		// assert
		Assert.True(result.Success);
		Assert.Equal(1, measurements.Sum("reality_scraper.listings.scraped", (ScraperTelemetry.Tags.ListingResult, "ok")));
		Assert.Equal(1, measurements.Sum("reality_scraper.listings.scraped", (ScraperTelemetry.Tags.ListingResult, "skipped")));
		Assert.Equal(0, measurements.Sum("reality_scraper.listings.scraped", (ScraperTelemetry.Tags.ListingResult, "failed")));
		Assert.Equal(1, measurements.Sum("reality_scraper.scraper.pages"));
		Assert.Equal(1, measurements.Count("reality_scraper.scraper.duration", (ScraperTelemetry.Tags.Outcome, ScraperTelemetry.Outcomes.Success)));

		var scrapeActivity = Assert.Single(activities.Stopped, a => a.OperationName == $"Scrape {siteName}");
		Assert.NotEqual(ActivityStatusCode.Error, scrapeActivity.Status);
		Assert.Contains(activities.Stopped, a => a.OperationName == "Parse page" && a.ParentSpanId == scrapeActivity.SpanId);
	}

	[Fact]
	public async Task ScrapeListingsAsync_DisallowedTarget_RecordsFailureOutcomeAndErrorSpan()
	{
		// arrange
		var siteName = $"telemetry-{Guid.NewGuid():N}";
		using var measurements = new MeasurementRecorder(siteName);
		using var activities = new ActivityRecorder(siteName);
		var sut = CreateSut(siteName, Mock.Of<IWebDriver>(), isPublicTarget: false);

		// act
		var result = await sut.ScrapeListingsAsync(new ScraperConfiguration { Url = ValidUrl }, CancellationToken.None);

		// assert
		Assert.False(result.Success);
		Assert.Equal(1, measurements.Count("reality_scraper.scraper.duration", (ScraperTelemetry.Tags.Outcome, ScraperTelemetry.Outcomes.Failure)));
		var scrapeActivity = Assert.Single(activities.Stopped, a => a.OperationName == $"Scrape {siteName}");
		Assert.Equal(ActivityStatusCode.Error, scrapeActivity.Status);
	}

	private static TestScraper CreateSut(string siteName, IWebDriver driver, bool isPublicTarget = true)
	{
		var webDriverFactory = new Mock<IWebDriverFactory>();
		webDriverFactory.Setup(x => x.CreateDriver()).Returns(driver);

		var urlSafetyValidator = new Mock<IUrlSafetyValidator>();
		urlSafetyValidator
			.Setup(x => x.IsPublicHttpTargetAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(isPublicTarget);

		return new TestScraper(siteName, webDriverFactory.Object, urlSafetyValidator.Object);
	}

	private static IWebDriverElement CreateListingCard(string? detailUrl)
	{
		var link = new Mock<IWebDriverElement>();
		link.Setup(x => x.GetAttributeAsync("href", It.IsAny<CancellationToken>())).ReturnsAsync(detailUrl);

		var text = new Mock<IWebDriverElement>();
		text.Setup(x => x.GetTextAsync(It.IsAny<CancellationToken>())).ReturnsAsync("text");

		var card = new Mock<IWebDriverElement>();
		card.Setup(x => x.FindElementAsync("a", It.IsAny<CancellationToken>())).ReturnsAsync(link.Object);
		card.Setup(x => x.FindElementAsync(It.IsIn("h2", "p", "span", "img"), It.IsAny<CancellationToken>())).ReturnsAsync(text.Object);
		return card.Object;
	}

	/// <summary>
	/// Sbírá měření instrumentů aplikace s tagem scraper.site daného portálu.
	/// </summary>
	private sealed class MeasurementRecorder : IDisposable
	{
		private readonly string siteName;
		private readonly MeterListener listener = new();
		private readonly ConcurrentBag<(string Instrument, double Value, Dictionary<string, object?> Tags)> measurements = new();

		public MeasurementRecorder(string siteName)
		{
			this.siteName = siteName;
			listener.InstrumentPublished = (instrument, l) =>
			{
				if (instrument.Meter.Name == ScraperTelemetry.Name)
				{
					l.EnableMeasurementEvents(instrument);
				}
			};
			listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(instrument, value, tags));
			listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(instrument, value, tags));
			listener.Start();
		}

		public double Sum(string instrument, params (string Key, string Value)[] tags)
		{
			return Matching(instrument, tags).Sum(m => m.Value);
		}

		public int Count(string instrument, params (string Key, string Value)[] tags)
		{
			return Matching(instrument, tags).Count();
		}

		private IEnumerable<(string Instrument, double Value, Dictionary<string, object?> Tags)> Matching(string instrument, (string Key, string Value)[] tags)
		{
			return measurements.Where(m => m.Instrument == instrument
				&& tags.All(t => m.Tags.TryGetValue(t.Key, out var v) && Equals(v, t.Value)));
		}

		private void Record(Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
		{
			var tagDictionary = new Dictionary<string, object?>();
			foreach (var tag in tags)
			{
				tagDictionary[tag.Key] = tag.Value;
			}

			if (tagDictionary.TryGetValue(ScraperTelemetry.Tags.Site, out var site) && Equals(site, siteName))
			{
				measurements.Add((instrument.Name, value, tagDictionary));
			}
		}

		public void Dispose() => listener.Dispose();
	}

	/// <summary>
	/// Sbírá dokončené aktivity aplikace v trace, jehož kořen patří danému portálu.
	/// </summary>
	private sealed class ActivityRecorder : IDisposable
	{
		private readonly ActivityListener listener;
		private readonly ConcurrentBag<Activity> stopped = new();

		public ActivityRecorder(string siteName)
		{
			listener = new ActivityListener
			{
				ShouldListenTo = source => source.Name == ScraperTelemetry.Name,
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
				ActivityStopped = activity =>
				{
					var root = activity;
					while (root.Parent != null)
					{
						root = root.Parent;
					}

					if (root.OperationName == $"Scrape {siteName}")
					{
						stopped.Add(activity);
					}
				}
			};
			ActivitySource.AddActivityListener(listener);
		}

		public IReadOnlyCollection<Activity> Stopped => stopped.ToArray();

		public void Dispose() => listener.Dispose();
	}

	private sealed class TestScraper : BaseScraperService
	{
		private readonly string siteName;

		public TestScraper(string siteName, IWebDriverFactory webDriverFactory, IUrlSafetyValidator urlSafetyValidator)
			: base(Mock.Of<ILogger>(), webDriverFactory, urlSafetyValidator)
		{
			this.siteName = siteName;
		}

		public override string SiteName => siteName;

		public override ScrapersEnum ScrapersEnum => ScrapersEnum.SReality;

		protected override BaseScraperOptions Options => new()
		{
			ListingSelector = "li",
			DetailLinkSelector = "a",
			TitleSelector = "h2",
			PriceSelector = "p",
			LocationSelector = "span",
			ImageSelector = "img",
			NextPageSelector = ".next"
		};

		protected override string ImageAttribute => "src";

		protected override Task<bool> NavigateToNextPageAsync(
			IWebDriver driver, string baseUrl, int currentPage,
			IReadOnlyList<IWebDriverElement> nextButtons, CancellationToken cancellationToken)
		{
			return Task.FromResult(false);
		}
	}
}

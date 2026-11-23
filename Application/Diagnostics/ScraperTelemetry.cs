using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace RealityScraper.Application.Diagnostics;

/// <summary>
/// Vlastní trace a metriky aplikace (OpenTelemetry). Bez zaregistrovaného listeneru
/// (export vypnutý) je zápis do instrumentů i start aktivit prakticky zdarma.
/// </summary>
/// <remarks>
/// Prometheus při OTLP ingestu převádí tečky na podtržítka a přidává jednotku a u čítačů
/// <c>_total</c> - např. <c>reality_scraper.task.duration</c> v sekundách se v Grafaně
/// hledá jako <c>reality_scraper_task_duration_seconds</c>.
/// </remarks>
public static class ScraperTelemetry
{
	public const string Name = "RealityScraper";

	public static class Tags
	{
		public const string TaskId = "task.id";
		public const string TaskName = "task.name";
		public const string TaskType = "task.type";
		public const string Outcome = "outcome";
		public const string Site = "scraper.site";
		public const string Page = "scraper.page";
		public const string ListingResult = "listing.result";
		public const string ListingChange = "listing.change";
		public const string RecipientCount = "email.recipient_count";
	}

	public static class Outcomes
	{
		public const string Success = "success";
		public const string Failure = "failure";
		public const string Canceled = "canceled";
	}

	public static readonly ActivitySource ActivitySource = new(Name);

	private static readonly Meter Meter = new(Name);

	// Běhy úloh trvají sekundy až desítky minut - výchozí hranice OTel (0-10 000) by
	// většinu běhů nacpaly do pár posledních bucketů.
	private static readonly InstrumentAdvice<double> LongRunningAdvice = new()
	{
		HistogramBucketBoundaries = [1, 5, 10, 30, 60, 120, 300, 600, 1200, 1800, 3600]
	};

	public static readonly Counter<long> TaskRuns = Meter.CreateCounter<long>(
		"reality_scraper.task.runs", "{run}", "Počet běhů plánovaných úloh podle typu a výsledku.");

	public static readonly Histogram<double> TaskDuration = Meter.CreateHistogram(
		"reality_scraper.task.duration", "s", "Délka běhu plánované úlohy.", advice: LongRunningAdvice);

	public static readonly Histogram<double> ScraperDuration = Meter.CreateHistogram(
		"reality_scraper.scraper.duration", "s", "Délka scrapování jednoho cíle na portálu.", advice: LongRunningAdvice);

	public static readonly Counter<long> ScraperPages = Meter.CreateCounter<long>(
		"reality_scraper.scraper.pages", "{page}", "Počet prošlých stránek výpisu.");

	public static readonly Counter<long> ListingsScraped = Meter.CreateCounter<long>(
		"reality_scraper.listings.scraped", "{listing}", "Karty inzerátů podle výsledku zpracování (ok / failed / skipped).");

	public static readonly Counter<long> ListingChanges = Meter.CreateCounter<long>(
		"reality_scraper.listings.changes", "{listing}", "Nové inzeráty a změny cen podle portálu.");

	public static readonly Counter<long> EmailsSent = Meter.CreateCounter<long>(
		"reality_scraper.emails.sent", "{email}", "Odeslané e-maily (jeden na příjemce) podle výsledku.");

	/// <summary>
	/// Označí aktivitu jako chybovou včetně výjimky (zobrazí se v Tempu u spanu).
	/// </summary>
	public static void SetError(this Activity? activity, Exception? exception = null, string? description = null)
	{
		if (activity == null)
		{
			return;
		}

		activity.SetStatus(ActivityStatusCode.Error, description ?? exception?.Message);
		if (exception != null)
		{
			activity.AddException(exception);
		}
	}
}

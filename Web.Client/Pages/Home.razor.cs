using System.Net.Http.Json;
using System.Text.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Shared;
using RealityScraper.Web.Shared.Models.Dashboard;
using RealityScraper.Web.Shared.Models.ReportTasks;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Client.Pages;

public partial class Home(
	HttpClient http,
	IHxMessengerService messenger,
	NavigationManager nav)
{
	/// <summary>Fallback pro popisky, než se načte skutečná délka okna z API.</summary>
	private const int DefaultWindowDays = 7;

	private DashboardSummaryResult? summary;
	private List<TaskRow> taskRows = [];
	private List<string> failedTaskNames = [];
	private bool loadingSummary = true;
	private bool loadingTasks = true;
	private Guid? runningTaskId;

	private int WindowDays => summary?.WindowDays ?? DefaultWindowDays;

	protected override async Task OnInitializedAsync()
	{
		// Při SSR prerenderu nemá serverový HttpClient přihlašovací cookie, API by odpovědělo
		// přesměrováním na login. Data se načtou až v interaktivním (WASM) renderu.
		if (!RendererInfo.IsInteractive)
		{
			return;
		}

		// Každé volání má vlastní ošetření chyby - výpadek seznamu úloh nesmí vyprázdnit i dlaždice.
		await Task.WhenAll(LoadSummaryAsync(), LoadTasksAsync());
	}

	private async Task LoadSummaryAsync()
	{
		loadingSummary = true;
		try
		{
			summary = await http.GetFromJsonAsync<DashboardSummaryResult>("/api/dashboard");
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst souhrn inzerátů.");
		}
		finally
		{
			loadingSummary = false;
		}
	}

	private async Task LoadTasksAsync()
	{
		loadingTasks = true;
		try
		{
			var scraperTasksRequest = http.GetFromJsonAsync<List<ScraperTaskResult>>("/api/scraper-tasks");
			var reportTasksRequest = http.GetFromJsonAsync<List<ReportTaskResult>>("/api/report-tasks");

			var scraperTasks = await scraperTasksRequest ?? [];
			var reportTasks = await reportTasksRequest ?? [];

			taskRows =
			[
				.. scraperTasks.Select(t => new TaskRow(
					t.Id, t.Name, "Scraper", t.CronExpression, t.Enabled, t.LastRunAt, t.NextRunAt, t.LastRunSucceeded,
					$"/scraper-tasks/{t.Id}/edit", $"/api/scraper-tasks/{t.Id}/run-now")),
				.. reportTasks.Select(t => new TaskRow(
					t.Id, t.Name, "Report", t.CronExpression, t.Enabled, t.LastRunAt, t.NextRunAt, t.LastRunSucceeded,
					$"/report-tasks/{t.Id}/edit", $"/api/report-tasks/{t.Id}/run-now"))
			];

			// Vypnutá úloha ani úloha, která ještě neběžela (LastRunSucceeded == null), není chyba.
			failedTaskNames = taskRows
				.Where(r => r.Enabled && r.LastRunSucceeded == false)
				.Select(r => r.Name)
				.ToList();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst stav úloh.");
		}
		finally
		{
			loadingTasks = false;
		}
	}

	private void HandleCreateTaskClick()
	{
		nav.NavigateTo("/scraper-tasks/create");
	}

	private async Task HandleRunNowClick(TaskRow row)
	{
		runningTaskId = row.Id;
		try
		{
			using var response = await http.PostAsync(row.RunNowUrl, null);
			if (response.IsSuccessStatusCode)
			{
				// Endpoint vrací 202 - úloha se spouští na pozadí, LastRunAt se hned nezmění.
				messenger.AddInformation($"Úloha '{row.Name}' byla naplánována ke spuštění.");
			}
			else
			{
				messenger.AddError($"Nepodařilo se spustit úlohu '{row.Name}'.");
			}
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
		{
			messenger.AddError($"Nepodařilo se spustit úlohu '{row.Name}'.");
		}
		finally
		{
			runningTaskId = null;
		}

		await LoadTasksAsync();
	}

	private List<KpiTile> BuildTiles()
	{
		return
		[
			new KpiTile(
				"Aktivní inzeráty",
				summary is null ? "—" : PriceFormatter.FormatCount(summary.ActiveCount),
				"celkem napříč úlohami",
				"Inzeráty, které jsou na portálu stále k vidění.",
				null),
			new KpiTile(
				"Nové",
				summary is null ? "—" : PriceFormatter.FormatSignedCount(summary.NewCount),
				$"za posledních {WindowDays} dní",
				$"Inzeráty poprvé zachycené za posledních {WindowDays} dní, i když už mezitím zmizely.",
				"text-primary"),
			new KpiTile(
				"Vyřazené",
				summary is null ? "—" : PriceFormatter.FormatSignedCount(-summary.RemovedCount),
				$"za posledních {WindowDays} dní",
				$"Inzeráty vyřazené za posledních {WindowDays} dní, které se do teď nevrátily zpět.",
				"text-muted"),
			new KpiTile(
				"Zlevněné",
				summary is null ? "—" : PriceFormatter.FormatCount(summary.PriceDropCount),
				$"za posledních {WindowDays} dní",
				$"Živé inzeráty, jejichž poslední cenová změna za posledních {WindowDays} dní byla zlevnění.",
				"text-success")
		];
	}

	private static decimal GetDifference(PriceDropResult drop)
	{
		// Aktuální cena je u zlevnění vždy vyplněná, dotaz na zlevnění inzeráty bez ceny odfiltruje.
		return (drop.Listing.Price ?? drop.PreviousPrice) - drop.PreviousPrice;
	}

	private static decimal? GetDifferencePercent(PriceDropResult drop)
	{
		return drop.PreviousPrice != 0
			? GetDifference(drop) / drop.PreviousPrice * 100
			: null;
	}

	private sealed record KpiTile(string Label, string Value, string Note, string Tooltip, string? ValueCssClass);

	private sealed record TaskRow(
		Guid Id,
		string Name,
		string KindLabel,
		string CronExpression,
		bool Enabled,
		DateTimeOffset? LastRunAt,
		DateTimeOffset? NextRunAt,
		bool? LastRunSucceeded,
		string DetailUrl,
		string RunNowUrl);
}
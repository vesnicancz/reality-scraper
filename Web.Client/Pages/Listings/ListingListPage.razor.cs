using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using RealityScraper.Web.Shared.Models.Listings;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Client.Pages.Listings;

public partial class ListingListPage(
	HttpClient http,
	IHxMessengerService messenger)
{
	private const int DefaultPageSize = 15;

	private static readonly CultureInfo czechCulture = CultureInfo.GetCultureInfo("cs-CZ");

	private static readonly List<StateFilterOption> stateFilterOptions =
	[
		new(true, "Aktivní"),
		new(false, "Neaktivní")
	];

	private HxGrid<ListingResult> grid = null!;
	private HxOffcanvas priceHistoryOffcanvas = null!;
	private List<ScraperTaskResult> scraperTasks = [];
	private bool? activeFilter = true;
	private Guid? scraperTaskFilter;
	private string? searchTerm;
	private List<PriceHistoryRow>? priceHistoryRows;
	private string priceHistoryOffcanvasTitle = "Historie cen";

	protected override async Task OnInitializedAsync()
	{
		try
		{
			scraperTasks = await http.GetFromJsonAsync<List<ScraperTaskResult>>("/api/scraper-tasks") ?? [];
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst seznam tasků pro filtr.");
		}
	}

	private async Task<GridDataProviderResult<ListingResult>> LoadData(GridDataProviderRequest<ListingResult> request)
	{
		try
		{
			var pageSize = request.Count ?? DefaultPageSize;
			var pageIndex = request.StartIndex / pageSize;

			var url = $"/api/listings?pageIndex={pageIndex}&pageSize={pageSize}";
			if (activeFilter.HasValue)
			{
				url += $"&isActive={(activeFilter.Value ? "true" : "false")}";
			}

			if (scraperTaskFilter.HasValue)
			{
				url += $"&scraperTaskId={scraperTaskFilter.Value}";
			}

			if (!string.IsNullOrWhiteSpace(searchTerm))
			{
				url += $"&search={Uri.EscapeDataString(searchTerm.Trim())}";
			}

			var page = await http.GetFromJsonAsync<ListingPageResult>(url, request.CancellationToken)
				?? new ListingPageResult();

			return new GridDataProviderResult<ListingResult>
			{
				Data = page.Items,
				TotalCount = page.TotalCount
			};
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst seznam realit.");
			return new GridDataProviderResult<ListingResult>
			{
				Data = [],
				TotalCount = 0
			};
		}
	}

	private async Task RefreshGridAsync()
	{
		await grid.RefreshDataAsync(GridStateResetOptions.ResetPosition);
	}

	private async Task HandleShowPriceHistoryClick(ListingResult listing)
	{
		try
		{
			var history = await http.GetFromJsonAsync<List<PriceHistoryResult>>($"/api/listings/{listing.Id}/price-history") ?? [];
			priceHistoryRows = BuildPriceHistoryRows(history);
			priceHistoryOffcanvasTitle = $"Historie cen – {listing.Title}";
			await priceHistoryOffcanvas.ShowAsync();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst historii cen.");
		}
	}

	private static List<PriceHistoryRow> BuildPriceHistoryRows(List<PriceHistoryResult> history)
	{
		var rows = new List<PriceHistoryRow>(history.Count);
		decimal? previousPrice = null;

		foreach (var record in history.OrderBy(h => h.RecordedAt))
		{
			decimal? difference = null;
			decimal? differencePercent = null;
			if (record.Price.HasValue && previousPrice.HasValue)
			{
				difference = record.Price.Value - previousPrice.Value;
				if (previousPrice.Value != 0)
				{
					differencePercent = difference.Value / previousPrice.Value * 100;
				}
			}

			rows.Add(new PriceHistoryRow(record.RecordedAt, record.Price, difference, differencePercent, record.IsCurrent));
			previousPrice = record.Price;
		}

		// nejnovější cena (aktuální) nahoře
		rows.Reverse();
		return rows;
	}

	private static string FormatPrice(decimal? price)
	{
		return price.HasValue
			? string.Create(czechCulture, $"{price.Value:N0} Kč")
			: "—";
	}

	private static string FormatSignedPrice(decimal difference)
	{
		var sign = difference > 0 ? "+" : "−";
		return string.Create(czechCulture, $"{sign}{Math.Abs(difference):N0} Kč");
	}

	private static string FormatSignedPercent(decimal percent)
	{
		var sign = percent > 0 ? "+" : "−";
		return string.Create(czechCulture, $"{sign}{Math.Abs(percent):N1} %");
	}

	private sealed record PriceHistoryRow(
		DateTimeOffset From,
		decimal? Price,
		decimal? Difference,
		decimal? DifferencePercent,
		bool IsCurrent);

	private record StateFilterOption(bool Value, string Name);
}

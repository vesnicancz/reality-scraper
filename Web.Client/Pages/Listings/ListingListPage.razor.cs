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
	private List<PriceHistoryResult>? selectedPriceHistory;
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
			selectedPriceHistory = await http.GetFromJsonAsync<List<PriceHistoryResult>>($"/api/listings/{listing.Id}/price-history");
			priceHistoryOffcanvasTitle = $"Historie cen – {listing.Title}";
			await priceHistoryOffcanvas.ShowAsync();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst historii cen.");
		}
	}

	private static string FormatPrice(decimal? price)
	{
		return price.HasValue
			? string.Create(czechCulture, $"{price.Value:N0} Kč")
			: "—";
	}

	private record StateFilterOption(bool Value, string Name);
}

using System.Net.Http.Json;
using System.Text.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Client.Pages.ScraperTasks;

public partial class ScraperTaskListPage(
	HttpClient http,
	IHxMessengerService messenger,
	IHxMessageBoxService messageBox,
	NavigationManager nav)
{
	private HxGrid<ScraperTaskResult> grid = null!;
	private HxOffcanvas logOffcanvas = null!;
	private string? selectedTaskLog;
	private string logOffcanvasTitle = "Logy";

	private async Task<GridDataProviderResult<ScraperTaskResult>> LoadData(GridDataProviderRequest<ScraperTaskResult> request)
	{
		try
		{
			var tasks = await http.GetFromJsonAsync<List<ScraperTaskResult>>("/api/scraper-tasks", request.CancellationToken)
				?? [];

			return new GridDataProviderResult<ScraperTaskResult>
			{
				Data = tasks,
				TotalCount = tasks.Count
			};
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst seznam tasků.");
			return new GridDataProviderResult<ScraperTaskResult>
			{
				Data = [],
				TotalCount = 0
			};
		}
	}

	private void HandleCreateClick()
	{
		nav.NavigateTo("/scraper-tasks/create");
	}

	private async Task HandleShowLogClick(ScraperTaskResult task)
	{
		try
		{
			var detail = await http.GetFromJsonAsync<ScraperTaskResult>($"/api/scraper-tasks/{task.Id}");
			selectedTaskLog = detail?.LastRunLog;
			logOffcanvasTitle = $"Logy – {task.Name}";
			await logOffcanvas.ShowAsync();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se načíst logy.");
		}
	}

	private void HandleEditClick(ScraperTaskResult task)
	{
		nav.NavigateTo($"/scraper-tasks/{task.Id}/edit");
	}

	private async Task HandleRunNowClick(ScraperTaskResult task)
	{
		try
		{
			using var response = await http.PostAsync($"/api/scraper-tasks/{task.Id}/run-now", null);
			if (response.IsSuccessStatusCode)
			{
				messenger.AddInformation($"Task '{task.Name}' byl naplánován ke spuštění.");
			}
			else
			{
				messenger.AddError($"Nepodařilo se spustit task '{task.Name}'.");
			}

			await grid.RefreshDataAsync();
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError($"Nepodařilo se spustit task '{task.Name}'.");
		}
	}

	private async Task HandleDeleteClick(ScraperTaskResult task)
	{
		var confirmed = await messageBox.ConfirmAsync("Smazat task", $"Opravdu chcete smazat task '{task.Name}'?");
		if (!confirmed)
		{
			return;
		}

		try
		{
			using var response = await http.DeleteAsync($"/api/scraper-tasks/{task.Id}");
			if (response.IsSuccessStatusCode)
			{
				messenger.AddInformation($"Task '{task.Name}' byl smazán.");
				await grid.RefreshDataAsync();
			}
			else
			{
				messenger.AddError("Nepodařilo se smazat task.");
			}
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se smazat task.");
		}
	}
}
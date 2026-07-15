using System.Net.Http.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Shared.Models.ReportTasks;

namespace RealityScraper.Web.Client.Pages.ReportTasks;

public partial class ReportTaskListPage(
	HttpClient http,
	IHxMessengerService messenger,
	NavigationManager nav)
{
	private HxGrid<ReportTaskResult> grid = null!;
	private HxOffcanvas logOffcanvas = null!;
	private string? selectedTaskLog;
	private string logOffcanvasTitle = "Logy";

	private async Task<GridDataProviderResult<ReportTaskResult>> LoadData(GridDataProviderRequest<ReportTaskResult> request)
	{
		try
		{
			var tasks = await http.GetFromJsonAsync<List<ReportTaskResult>>("/api/report-tasks", request.CancellationToken)
				?? [];

			return new GridDataProviderResult<ReportTaskResult>
			{
				Data = tasks,
				TotalCount = tasks.Count
			};
		}
		catch (HttpRequestException)
		{
			messenger.AddError("Nepodařilo se načíst seznam reportů.");
			return new GridDataProviderResult<ReportTaskResult>
			{
				Data = [],
				TotalCount = 0
			};
		}
	}

	private void HandleCreateClick()
	{
		nav.NavigateTo("/report-tasks/create");
	}

	private async Task HandleShowLogClick(ReportTaskResult task)
	{
		try
		{
			var detail = await http.GetFromJsonAsync<ReportTaskResult>($"/api/report-tasks/{task.Id}");
			selectedTaskLog = detail?.LastRunLog;
			logOffcanvasTitle = $"Logy – {task.Name}";
			await logOffcanvas.ShowAsync();
		}
		catch (HttpRequestException)
		{
			messenger.AddError("Nepodařilo se načíst logy.");
		}
	}

	private void HandleEditClick(ReportTaskResult task)
	{
		nav.NavigateTo($"/report-tasks/{task.Id}/edit");
	}

	private async Task HandleRunNowClick(ReportTaskResult task)
	{
		try
		{
			using var response = await http.PostAsync($"/api/report-tasks/{task.Id}/run-now", null);
			if (response.IsSuccessStatusCode)
			{
				messenger.AddInformation($"Report '{task.Name}' byl naplánován ke spuštění.");
			}
			else
			{
				messenger.AddError($"Nepodařilo se spustit report '{task.Name}'.");
			}

			await grid.RefreshDataAsync();
		}
		catch (HttpRequestException)
		{
			messenger.AddError($"Nepodařilo se spustit report '{task.Name}'.");
		}
	}

	private async Task HandleDeleteClick(ReportTaskResult task)
	{
		try
		{
			using var response = await http.DeleteAsync($"/api/report-tasks/{task.Id}");
			if (response.IsSuccessStatusCode)
			{
				messenger.AddInformation($"Report '{task.Name}' byl smazán.");
				await grid.RefreshDataAsync();
			}
			else
			{
				messenger.AddError("Nepodařilo se smazat report.");
			}
		}
		catch (HttpRequestException)
		{
			messenger.AddError("Nepodařilo se smazat report.");
		}
	}
}

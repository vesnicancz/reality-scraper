using System.Net.Http.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Shared.Models.ReportTasks;
using RealityScraper.Web.Shared.Models.ScraperTasks;

namespace RealityScraper.Web.Client.Pages.ReportTasks;

public partial class ReportTaskEditPage(
	HttpClient http,
	IHxMessengerService messenger,
	NavigationManager nav)
{
	[Parameter]
	public Guid? Id { get; set; }

	private bool IsEdit => Id.HasValue;
	private bool isLoading;
	private ReportTaskFormModel model = new();
	private List<ScraperTaskOption> scraperTaskOptions = [];
	private bool dummyCheckboxValue;

	protected override async Task OnInitializedAsync()
	{
		isLoading = true;
		try
		{
			var scraperTasks = await http.GetFromJsonAsync<List<ScraperTaskResult>>("/api/scraper-tasks") ?? [];
			scraperTaskOptions = scraperTasks.Select(t => new ScraperTaskOption(t.Id, t.Name)).ToList();

			if (IsEdit)
			{
				var task = await http.GetFromJsonAsync<ReportTaskResult>($"/api/report-tasks/{Id}");
				if (task == null)
				{
					messenger.AddError("Report nebyl nalezen.");
					nav.NavigateTo("/report-tasks");
					return;
				}

				model = new ReportTaskFormModel
				{
					Name = task.Name,
					CronExpression = task.CronExpression,
					Enabled = task.Enabled,
					Recipients = task.Recipients.Select(email => new RecipientInputModel { Email = email }).ToList(),
					ScraperTaskIds = task.Sources.Select(s => s.ScraperTaskId).ToList()
				};
			}
		}
		catch (HttpRequestException)
		{
			messenger.AddError("Nepodařilo se načíst data.");
			nav.NavigateTo("/report-tasks");
		}
		finally
		{
			isLoading = false;
		}
	}

	private async Task HandleValidSubmit()
	{
		try
		{
			HttpResponseMessage response;

			if (IsEdit)
			{
				response = await http.PutAsJsonAsync($"/api/report-tasks/{Id}", new UpdateReportTaskRequest
				{
					Name = model.Name,
					CronExpression = model.CronExpression,
					Enabled = model.Enabled,
					Recipients = model.Recipients,
					ScraperTaskIds = model.ScraperTaskIds
				});
			}
			else
			{
				response = await http.PostAsJsonAsync("/api/report-tasks", new CreateReportTaskRequest
				{
					Name = model.Name,
					CronExpression = model.CronExpression,
					Enabled = model.Enabled,
					Recipients = model.Recipients,
					ScraperTaskIds = model.ScraperTaskIds
				});
			}

			if (response.IsSuccessStatusCode)
			{
				messenger.AddInformation(IsEdit ? "Report byl upraven." : "Report byl vytvořen.");
				nav.NavigateTo("/report-tasks");
			}
			else
			{
				messenger.AddError("Nepodařilo se uložit report.");
			}
		}
		catch (HttpRequestException)
		{
			messenger.AddError("Nepodařilo se uložit report.");
		}
	}

	private void AddRecipient()
	{
		model.Recipients.Add(new RecipientInputModel());
	}

	private void RemoveRecipient(int index)
	{
		model.Recipients.RemoveAt(index);
	}

	private void ToggleScraperTask(Guid scraperTaskId, bool selected)
	{
		if (selected)
		{
			if (!model.ScraperTaskIds.Contains(scraperTaskId))
			{
				model.ScraperTaskIds.Add(scraperTaskId);
			}
		}
		else
		{
			model.ScraperTaskIds.Remove(scraperTaskId);
		}
	}

	private void HandleCancel()
	{
		nav.NavigateTo("/report-tasks");
	}

	private class ReportTaskFormModel : ReportTaskInputModel;

	private record ScraperTaskOption(Guid Id, string Name);
}

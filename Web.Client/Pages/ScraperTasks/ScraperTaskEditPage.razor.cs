using System.Net.Http.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Shared.Models.ScraperTasks;
using RealityScraper.Web.Shared.Models.ScraperTaskRecipients;
using RealityScraper.Web.Shared.Models.ScraperTaskTargets;

namespace RealityScraper.Web.Client.Pages.ScraperTasks;

public partial class ScraperTaskEditPage(
	HttpClient http,
	IHxMessengerService messenger,
	NavigationManager nav)
{
	[Parameter]
	public Guid? Id { get; set; }

	private bool IsEdit => Id.HasValue;
	private bool isLoading;
	private ScraperTaskFormModel model = new();

	private readonly List<ScraperTypeOption> scraperTypeOptions =
	[
		new(1, "SReality"),
		new(2, "Reality Idnes")
	];

	protected override async Task OnInitializedAsync()
	{
		if (IsEdit)
		{
			isLoading = true;
			var task = await http.GetFromJsonAsync<ScraperTaskResult>($"/api/scraper-tasks/{Id}");
			if (task != null)
			{
				model = new ScraperTaskFormModel
				{
					Name = task.Name,
					CronExpression = task.CronExpression,
					Enabled = task.Enabled,
					Recipients = task.Recipients.Select(r => new RecipientInputModel { Email = r.Email }).ToList(),
					Targets = task.Targets.Select(t => new TargetInputModel { ScraperType = t.ScraperType, Url = t.Url }).ToList()
				};
			}
			isLoading = false;
		}
	}

	private async Task HandleValidSubmit()
	{
		HttpResponseMessage response;

		if (IsEdit)
		{
			response = await http.PutAsJsonAsync($"/api/scraper-tasks/{Id}", new UpdateScraperTaskRequest
			{
				Name = model.Name,
				CronExpression = model.CronExpression,
				Enabled = model.Enabled,
				Recipients = model.Recipients,
				Targets = model.Targets
			});
		}
		else
		{
			response = await http.PostAsJsonAsync("/api/scraper-tasks", new CreateScraperTaskRequest
			{
				Name = model.Name,
				CronExpression = model.CronExpression,
				Enabled = model.Enabled,
				Recipients = model.Recipients,
				Targets = model.Targets
			});
		}

		if (response.IsSuccessStatusCode)
		{
			messenger.AddInformation(IsEdit ? "Task byl upraven." : "Task byl vytvoren.");
			nav.NavigateTo("/scraper-tasks");
		}
		else
		{
			messenger.AddError("Nepodarilo se ulozit task.");
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

	private void AddTarget()
	{
		model.Targets.Add(new TargetInputModel { ScraperType = 1 });
	}

	private void RemoveTarget(int index)
	{
		model.Targets.RemoveAt(index);
	}

	private void HandleCancel()
	{
		nav.NavigateTo("/scraper-tasks");
	}

	private class ScraperTaskFormModel
	{
		public string Name { get; set; } = string.Empty;
		public string CronExpression { get; set; } = string.Empty;
		public bool Enabled { get; set; } = true;
		public List<RecipientInputModel> Recipients { get; set; } = [];
		public List<TargetInputModel> Targets { get; set; } = [];
	}

	private record ScraperTypeOption(int Value, string Name);
}
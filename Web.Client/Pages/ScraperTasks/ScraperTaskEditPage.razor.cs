using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using RealityScraper.Web.Client.Infrastructure;
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
	private bool isSubmitting;
	private ScraperTaskFormModel model = new();

	private static readonly List<ScraperTypeOption> scraperTypeOptions =
		Enum.GetValues<ScraperType>()
			.Select(type => new ScraperTypeOption((int)type, ScraperTypeDisplay.GetName(type)))
			.ToList();

	protected override async Task OnInitializedAsync()
	{
		if (IsEdit)
		{
			isLoading = true;
			try
			{
				var task = await http.GetFromJsonAsync<ScraperTaskResult>($"/api/scraper-tasks/{Id}");
				if (task == null)
				{
					messenger.AddError("Task nebyl nalezen.");
					nav.NavigateTo("/scraper-tasks");
					return;
				}

				model = new ScraperTaskFormModel
				{
					Name = task.Name,
					CronExpression = task.CronExpression,
					Enabled = task.Enabled,
					Recipients = task.Recipients.Select(r => new RecipientInputModel { Email = r.Email }).ToList(),
					Targets = task.Targets.Select(t => new TargetInputModel { ScraperType = t.ScraperType, Url = t.Url }).ToList()
				};
			}
			catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
			{
				messenger.AddError("Nepodařilo se načíst task.");
				nav.NavigateTo("/scraper-tasks");
			}
			finally
			{
				isLoading = false;
			}
		}
	}

	private async Task HandleValidSubmit()
	{
		if (isSubmitting)
		{
			return;
		}

		isSubmitting = true;
		try
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
				messenger.AddInformation(IsEdit ? "Task byl upraven." : "Task byl vytvořen.");
				nav.NavigateTo("/scraper-tasks");
			}
			else
			{
				await response.ShowErrorAsync(messenger, "Nepodařilo se uložit task.");
			}
		}
		catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
		{
			messenger.AddError("Nepodařilo se uložit task.");
		}
		finally
		{
			isSubmitting = false;
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
		model.Targets.Add(new TargetInputModel { ScraperType = (int)ScraperType.SReality });
	}

	private void RemoveTarget(int index)
	{
		model.Targets.RemoveAt(index);
	}

	private void HandleCancel()
	{
		nav.NavigateTo("/scraper-tasks");
	}

	private class ScraperTaskFormModel : ScraperTaskInputModel;

	private record ScraperTypeOption(int Value, string Name);
}
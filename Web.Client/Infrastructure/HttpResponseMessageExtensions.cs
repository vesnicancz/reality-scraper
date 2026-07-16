using System.Net.Http.Json;
using System.Text.Json;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;

namespace RealityScraper.Web.Client.Infrastructure;

internal static class HttpResponseMessageExtensions
{
	private static readonly JsonSerializerOptions ProblemDetailsSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public static async Task ShowErrorAsync(this HttpResponseMessage response, IHxMessengerService messenger, string fallbackMessage)
	{
		var validationMessages = await TryReadValidationMessagesAsync(response);

		if (validationMessages.Count > 0)
		{
			foreach (var message in validationMessages)
			{
				messenger.AddError(message);
			}
		}
		else
		{
			messenger.AddError(fallbackMessage);
		}
	}

	private static async Task<List<string>> TryReadValidationMessagesAsync(HttpResponseMessage response)
	{
		var messages = new List<string>();

		if ((int)response.StatusCode is < 400 or >= 500)
		{
			return messages;
		}

		try
		{
			var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetailsDto>(ProblemDetailsSerializerOptions);

			if (problemDetails?.Errors is { Length: > 0 } errors)
			{
				foreach (var error in errors)
				{
					if (!string.IsNullOrWhiteSpace(error.Description))
					{
						messages.Add(error.Description);
					}
				}
			}
			else if (!string.IsNullOrWhiteSpace(problemDetails?.Detail))
			{
				messages.Add(problemDetails.Detail);
			}
		}
		catch (Exception ex) when (ex is JsonException or NotSupportedException)
		{
		}

		return messages;
	}

	private sealed class ValidationProblemDetailsDto
	{
		public string? Detail { get; set; }

		public ValidationErrorDto[]? Errors { get; set; }
	}

	private sealed class ValidationErrorDto
	{
		public string? Description { get; set; }
	}
}

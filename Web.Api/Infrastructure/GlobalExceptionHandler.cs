using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RealityScraper.Web.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
#pragma warning disable CA1848 // Use the LoggerMessage delegates
		logger.LogError(exception, "Unhandled exception occurred");
#pragma warning restore CA1848 // Use the LoggerMessage delegates

		var problemDetails = new ProblemDetails
		{
			Status = StatusCodes.Status500InternalServerError,
			Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
			Title = "Server failure"
		};

		httpContext.Response.StatusCode = problemDetails.Status.Value;

		await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

		return true;
	}
}
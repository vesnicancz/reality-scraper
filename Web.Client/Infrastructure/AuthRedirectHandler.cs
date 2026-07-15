using System.Net;
using Microsoft.AspNetCore.Components;

namespace RealityScraper.Web.Client.Infrastructure;

internal sealed class AuthRedirectHandler(NavigationManager nav) : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken);

		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			var relativePath = "/" + nav.ToBaseRelativePath(nav.Uri);
			nav.NavigateTo($"/account/login?returnUrl={Uri.EscapeDataString(relativePath)}", forceLoad: true);
		}

		return response;
	}
}

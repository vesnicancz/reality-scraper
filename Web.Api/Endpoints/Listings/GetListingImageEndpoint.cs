using Microsoft.AspNetCore.Diagnostics;
using RealityScraper.Application.Interfaces.Scraping;

namespace RealityScraper.Web.Api.Endpoints.Listings;

internal sealed class GetListingImageEndpoint : IEndpoint
{
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		app.MapGet("/api/listings/{id:guid}/image", async (
			Guid id,
			IListingImageReader listingImageReader,
			HttpContext httpContext,
			CancellationToken cancellationToken) =>
		{
			var imageBytes = await listingImageReader.TryReadImageAsync(id, cancellationToken);
			if (imageBytes == null)
			{
				// 404 je pro grid signál, aby zkusil hotlink na portál - viz listingThumbnailFallback v app.js.
				// Bez vypnutí status code pages by se místo prázdné odpovědi překreslila celá SPA
				// (~11 kB HTML), a to u každého chybějícího náhledu na stránce.
				httpContext.Features.Get<IStatusCodePagesFeature>()?.Enabled = false;
				return Results.NotFound();
			}

			// Snímek se mění jen při výměně titulní fotky na portálu a scrape běží řádově jednou denně.
			httpContext.Response.Headers.CacheControl = "private, max-age=3600";

			return Results.File(imageBytes, "image/jpeg");
		});
	}
}

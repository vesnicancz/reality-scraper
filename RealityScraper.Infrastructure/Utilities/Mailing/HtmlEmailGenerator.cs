using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class HtmlEmailGenerator : IEmailGenerator
{
	public Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken)
	{
		var body = new System.Text.StringBuilder();
		body.AppendLine("<!DOCTYPE html>");
		body.AppendLine("<html>");
		body.AppendLine("<head>");
		body.AppendLine("<style>");
		body.AppendLine("body { font-family: Arial, sans-serif; }");
		body.AppendLine(".listing { margin-bottom: 20px; border-bottom: 1px solid #ccc; padding-bottom: 15px; }");
		body.AppendLine(".listing img { max-width: 300px; max-height: 200px; }");
		body.AppendLine("</style>");
		body.AppendLine("</head>");
		body.AppendLine("<body>");
		body.AppendLine("<h1>Nové realitní nabídky</h1>");
		body.AppendLine($"<p>Datum: {DateTime.Now:dd.MM.yyyy HH:mm}</p>");
		body.AppendLine($"<p>Celkem nalezeno: {GetPluralForm(scrapingReport.NewListingCount, "nová nabídka", "nové nabídky", "nových nabídek")}</p>");

		foreach (var result in scrapingReport.Results.Where(i => i.NewListingCount > 0 || i.PriceChangedListingsCount > 0))
		{
			body.AppendLine("<h2>" + result.SiteName + "</h2>");
			body.AppendLine($"<p>Celkem nalezeno: {GetPluralForm(result.TotalListingCount, "nabídka", "nabídky", "nabídek")}</p>");

			if (result.NewListingCount > 0)
			{
				body.AppendLine($"<p>Celkem nových: {result.NewListingCount}</p>");

				foreach (var listing in result.NewListings)
				{
					body.AppendLine("<div class='listing'>");
					body.AppendLine($"<h2>{listing.Title}</h2>");
					body.AppendLine($"<p><strong>Cena:</strong> {listing.Price?.ToString("C0") ?? "Neuvedeno"}</p>");
					body.AppendLine($"<p><strong>Lokalita:</strong> {listing.Location}</p>");

					if (!string.IsNullOrEmpty(listing.ImageUrl))
					{
						body.AppendLine($"<p><img src='{listing.ImageUrl}' alt='{listing.Title}'></p>");
					}

					body.AppendLine($"<p><a href='{listing.Url}' target='_blank'>Zobrazit detail nabídky</a></p>");
					body.AppendLine("</div>");
				}
			}
			else
			{
				body.AppendLine("<p>Žádné nové nabídky.</p>");
			}

			if (result.PriceChangedListingsCount > 0)
			{
				body.AppendLine($"<p>Celkem změn ceny: {result.PriceChangedListingsCount}</p>");

				foreach (var listing in result.PriceChangedListings)
				{
					body.AppendLine("<div class='listing'>");
					body.AppendLine($"<h2>{listing.Title}</h2>");
					body.AppendLine($"<p><strong>Stará cena:</strong> {listing.OldPrice?.ToString("C0") ?? "Neuvedeno"}</p>");
					body.AppendLine($"<p><strong>Nová cena:</strong> {listing.Price?.ToString("C0") ?? "Neuvedeno"}</p>");
					body.AppendLine($"<p><strong>Lokalita:</strong> {listing.Location}</p>");
					if (!string.IsNullOrEmpty(listing.ImageUrl))
					{
						body.AppendLine($"<p><img src='{listing.ImageUrl}' alt='{listing.Title}'></p>");
					}
					body.AppendLine($"<p><a href='{listing.Url}' target='_blank'>Zobrazit detail nabídky</a></p>");
					body.AppendLine("</div>");
				}
			}
		}

		body.AppendLine("</body>");
		body.AppendLine("</html>");
		return Task.FromResult(body.ToString());
	}

	public static string GetPluralForm(int count, string form1, string form2to4, string form5plus)
	{
		if (count == 1)
		{
			return $"{count} {form1}";
		}
		else if (count >= 2 && count <= 4)
		{
			return $"{count} {form2to4}";
		}
		else
		{
			return $"{count} {form5plus}";
		}
	}
}
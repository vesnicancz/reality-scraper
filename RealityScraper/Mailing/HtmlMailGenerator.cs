using RealityScraper.Scraping.Model;

namespace RealityScraper.Mailing;

public class HtmlMailGenerator : IHtmlMailGenerator
{
	public string GenerateHtmlBody(ScrapingReport scrapingReport)
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
		body.AppendLine($"<p>Celkem nalezeno: {scrapingReport.NewListingCount} nových nabídek</p>");

		foreach (var result in scrapingReport.Results.Where(i => i.NewListingCount > 0 || i.PriceChangedListingsCount > 0))
		{
			body.AppendLine("<h2>" + result.SiteName + "</h2>");
			body.AppendLine($"<p>Celkem nalezeno: {result.TotalListingCount} nabídek</p>");

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

			if (result.PriceChangedListings.Count > 0)
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
		return body.ToString();
	}
}
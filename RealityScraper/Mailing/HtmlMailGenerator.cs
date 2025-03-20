﻿using RealityScraper.Model;

namespace RealityScraper.Mailing;

public class HtmlMailGenerator : IHtmlMailGenerator
{
	public string GenerateHtmlBody(List<Listing> listings)
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
		body.AppendLine($"<p>Celkem nalezeno: {listings.Count} nových nabídek</p>");

		foreach (var listing in listings)
		{
			body.AppendLine("<div class='listing'>");
			body.AppendLine($"<h2>{listing.Title}</h2>");
			body.AppendLine($"<p><strong>Cena:</strong> {listing.Price?.ToString("C0")}</p>");
			body.AppendLine($"<p><strong>Lokalita:</strong> {listing.Location}</p>");

			if (!string.IsNullOrEmpty(listing.ImageUrl))
			{
				body.AppendLine($"<p><img src='{listing.ImageUrl}' alt='{listing.Title}'></p>");
			}

			body.AppendLine($"<p><a href='{listing.Url}' target='_blank'>Zobrazit detail nabídky</a></p>");
			body.AppendLine("</div>");
		}

		body.AppendLine("</body>");
		body.AppendLine("</html>");
		return body.ToString();
	}
}
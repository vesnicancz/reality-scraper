﻿using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class RazorEmailGenerator : IEmailGenerator
{
	private readonly RazorEngineCore.RazorEngine razorEngine;
	private readonly string templateDirectory;

	public RazorEmailGenerator()
	{
		razorEngine = new RazorEngineCore.RazorEngine();
		templateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Mailing", "Templates");
	}

	public async Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken)
	{
		var templateFileName = "ListingReport.cshtml";

		// Načtení šablony ze souboru
		string templatePath = Path.Combine(templateDirectory, templateFileName);
		string templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);

		// Kompilace šablony
		var compiledTemplate = await razorEngine.CompileAsync(templateContent, cancellationToken: cancellationToken);

		// Generování HTML
		return compiledTemplate.Run(scrapingReport);
	}
}
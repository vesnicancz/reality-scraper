using Microsoft.Extensions.Logging;
using RazorEngineCore;
using RealityScraper.Application.Features.Scraping.Model;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class RazorEmailGenerator : IEmailGenerator
{
	private const string TemplateFileName = "ListingReport.cshtml";

	private readonly IRazorEngine razorEngine;
	private readonly ILogger<RazorEmailGenerator> logger;

	public RazorEmailGenerator(IRazorEngine razorEngine, ILogger<RazorEmailGenerator> logger)
	{
		this.razorEngine = razorEngine;
		this.logger = logger;
	}

	public async Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken)
	{
		// Načtení šablony ze souboru
		string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Mailing", "Templates", TemplateFileName);
		logger.LogTrace("Loading template from {TemplatePath}", templatePath);
		string templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);

		// Kompilace šablony
		var compiledTemplate = await razorEngine.CompileAsync(templateContent, cancellationToken: cancellationToken);

		// Generování HTML
		return compiledTemplate.Run(scrapingReport);
	}
}
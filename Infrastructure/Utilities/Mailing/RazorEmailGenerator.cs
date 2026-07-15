using Microsoft.Extensions.Logging;
using RazorEngineCore;
using RealityScraper.Application.Features.Reporting.Model;
using RealityScraper.Application.Features.Scraping.Model.Report;
using RealityScraper.Application.Interfaces.Mailing;

namespace RealityScraper.Infrastructure.Utilities.Mailing;

public class RazorEmailGenerator : IEmailGenerator
{
	private const string ListingReportTemplateFileName = "ListingReport.cshtml";
	private const string RemovedListingsTemplateFileName = "RemovedListingsReport.cshtml";

	private readonly IRazorEngine razorEngine;
	private readonly ILogger<RazorEmailGenerator> logger;

	public RazorEmailGenerator(IRazorEngine razorEngine, ILogger<RazorEmailGenerator> logger)
	{
		this.razorEngine = razorEngine;
		this.logger = logger;
	}

	public Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport, CancellationToken cancellationToken)
	{
		return RenderTemplateAsync(ListingReportTemplateFileName, scrapingReport, cancellationToken);
	}

	public Task<string> GenerateRemovedListingsHtmlAsync(RemovedListingsReport report, CancellationToken cancellationToken)
	{
		return RenderTemplateAsync(RemovedListingsTemplateFileName, report, cancellationToken);
	}

	private async Task<string> RenderTemplateAsync(string templateFileName, object model, CancellationToken cancellationToken)
	{
		// Načtení šablony ze souboru
		string templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Utilities", "Mailing", "Templates", templateFileName);
		logger.LogTrace("Loading template from {TemplatePath}", templatePath);
		string templateContent = await File.ReadAllTextAsync(templatePath, cancellationToken);

		// Kompilace šablony
		var compiledTemplate = await razorEngine.CompileAsync(templateContent, cancellationToken: cancellationToken);

		// Generování HTML
		return compiledTemplate.Run(model);
	}
}

using RealityScraper.Scraping.Model;

namespace RealityScraper.Mailing;

public class RazorEmailGenerator : IEmailGenerator
{
	private readonly RazorEngineCore.RazorEngine razorEngine;
	private readonly string templateDirectory;

	public RazorEmailGenerator()
	{
		razorEngine = new RazorEngineCore.RazorEngine();
		templateDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Mailing", "Templates");
	}

	public async Task<string> GenerateHtmlBodyAsync(ScrapingReport scrapingReport)
	{
		var templateFileName = "ListingReport.cshtml";

		// Načtení šablony ze souboru
		string templatePath = Path.Combine(templateDirectory, templateFileName);
		string templateContent = await File.ReadAllTextAsync(templatePath);

		// Kompilace šablony
		var compiledTemplate = razorEngine.Compile(templateContent);

		// Generování HTML
		return compiledTemplate.Run(scrapingReport);
	}
}
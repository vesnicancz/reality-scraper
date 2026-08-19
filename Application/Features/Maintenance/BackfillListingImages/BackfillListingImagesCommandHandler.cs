using RealityScraper.Application.Abstractions.Messaging;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Application.Interfaces.Scraping;
using RealityScraper.SharedKernel;

namespace RealityScraper.Application.Features.Maintenance.BackfillListingImages;

internal sealed class BackfillListingImagesCommandHandler
	: ICommandHandler<BackfillListingImagesCommand, BackfillListingImagesResult>
{
	// Pojistka, aby request nevisel, kdyby chybějících snímků bylo proti očekávání hodně.
	// Zbytek se dotáhne dalším zavoláním - RemainingCount řekne, kolik jich ještě zbývá.
	private const int MaxDownloadsPerRun = 200;

	private readonly IListingRepository listingRepository;
	private readonly IListingImageReader listingImageReader;
	private readonly IImageDownloadService imageDownloadService;

	public BackfillListingImagesCommandHandler(
		IListingRepository listingRepository,
		IListingImageReader listingImageReader,
		IImageDownloadService imageDownloadService)
	{
		this.listingRepository = listingRepository;
		this.listingImageReader = listingImageReader;
		this.imageDownloadService = imageDownloadService;
	}

	public async Task<Result<BackfillListingImagesResult>> Handle(
		BackfillListingImagesCommand command,
		CancellationToken cancellationToken)
	{
		var listings = await listingRepository.GetActiveWithImageUrlAsync(cancellationToken);

		var downloadedCount = 0;
		var failedCount = 0;
		var remainingCount = 0;

		foreach (var listing in listings)
		{
			if (listingImageReader.ImageExists(listing.Id))
			{
				continue;
			}

			if (downloadedCount + failedCount >= MaxDownloadsPerRun)
			{
				remainingCount++;
				continue;
			}

			try
			{
				await imageDownloadService.DownloadImageAsync(listing, cancellationToken);

				// DownloadImageAsync vrací void a tiše přeskakuje neplatnou URL, nepovolený
				// cíl i odpověď bez Content-Type "image/*" - jediný spolehlivý test úspěchu
				// je existence souboru po volání.
				if (listingImageReader.ImageExists(listing.Id))
				{
					downloadedCount++;
				}
				else
				{
					failedCount++;
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// Chyba u jednoho inzerátu nesmí zastavit dotahování ostatních.
				failedCount++;
			}
		}

		return Result.Success(new BackfillListingImagesResult(
			listings.Count,
			downloadedCount,
			failedCount,
			remainingCount));
	}
}

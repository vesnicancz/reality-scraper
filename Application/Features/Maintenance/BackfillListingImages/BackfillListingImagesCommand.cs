using RealityScraper.Application.Abstractions.Messaging;

namespace RealityScraper.Application.Features.Maintenance.BackfillListingImages;

/// <summary>
/// Dotáhne chybějící náhledové obrázky k živým inzerátům z jejich uložené URL.
/// </summary>
public record BackfillListingImagesCommand : ICommand<BackfillListingImagesResult>;

public record BackfillListingImagesResult(
	int CheckedCount,
	int DownloadedCount,
	int FailedCount,
	int RemainingCount);

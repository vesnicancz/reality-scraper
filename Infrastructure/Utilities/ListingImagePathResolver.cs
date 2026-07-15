using Microsoft.Extensions.Configuration;

namespace RealityScraper.Infrastructure.Utilities;

/// <summary>
/// Jediné místo, které zná strukturu lokálního úložiště obrázků inzerátů:
/// {cwd}/{FileStorage:ImagePath}/{prvni-2-znaky-id}/{id}.jpg
/// </summary>
public class ListingImagePathResolver
{
	private const string DefaultImagePath = "files/images";

	private readonly IConfiguration configuration;

	public ListingImagePathResolver(IConfiguration configuration)
	{
		this.configuration = configuration;
	}

	public string GetImageFilePath(Guid listingId)
	{
		return Path.Combine(GetImageFolderPath(listingId), $"{listingId}.jpg");
	}

	public string GetImageFolderPath(Guid listingId)
	{
		var rootPath = configuration.GetValue<string>("FileStorage:ImagePath");
		if (string.IsNullOrWhiteSpace(rootPath))
		{
			rootPath = DefaultImagePath;
		}

		return Path.Combine(Directory.GetCurrentDirectory(), rootPath, listingId.ToString()[..2]);
	}
}

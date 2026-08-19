using Microsoft.Extensions.Configuration;
using RealityScraper.Infrastructure.Utilities;

namespace RealityScraper.Infrastructure.Tests.Utilities;

public class ListingImagePathResolverTests
{
	private static ListingImagePathResolver CreateSut(string? imagePath)
	{
		var settings = new Dictionary<string, string?>();
		if (imagePath != null)
		{
			settings["FileStorage:ImagePath"] = imagePath;
		}

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(settings)
			.Build();

		return new ListingImagePathResolver(configuration);
	}

	[Fact]
	public void GetImageFolderPath_ShardsByFirstTwoCharactersOfId()
	{
		// Arrange
		// Rozdělení do podsložek drží počet souborů v jedné složce rozumný.
		var listingId = new Guid("ab123456-0000-0000-0000-000000000000");
		var sut = CreateSut("files/images");

		// Act
		var folder = sut.GetImageFolderPath(listingId);

		// Assert
		Assert.Equal("ab", Path.GetFileName(folder));
	}

	[Fact]
	public void GetImageFilePath_IsFolderPathPlusIdWithJpgExtension()
	{
		// Arrange
		var listingId = Guid.NewGuid();
		var sut = CreateSut("files/images");

		// Act
		var filePath = sut.GetImageFilePath(listingId);

		// Assert
		// Porovnává se přes GetFullPath - resolver nechává oddělovače z konfigurace tak, jak jsou.
		Assert.Equal(
			Path.GetFullPath(sut.GetImageFolderPath(listingId)),
			Path.GetFullPath(Path.GetDirectoryName(filePath)!));
		Assert.Equal($"{listingId}.jpg", Path.GetFileName(filePath));
	}

	[Fact]
	public void GetImageFolderPath_UsesConfiguredRoot()
	{
		// Arrange
		var listingId = Guid.NewGuid();
		var sut = CreateSut("custom/storage");

		// Act
		var folder = sut.GetImageFolderPath(listingId);

		// Assert
		var expected = Path.Combine(Directory.GetCurrentDirectory(), "custom/storage", listingId.ToString()[..2]);
		Assert.Equal(expected, folder);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("   ")]
	public void GetImageFolderPath_FallsBackToDefaultRoot_WhenNotConfigured(string? configuredPath)
	{
		// Arrange
		var listingId = Guid.NewGuid();
		var sut = CreateSut(configuredPath);

		// Act
		var folder = sut.GetImageFolderPath(listingId);

		// Assert
		var expected = Path.Combine(Directory.GetCurrentDirectory(), "files/images", listingId.ToString()[..2]);
		Assert.Equal(expected, folder);
	}

	[Fact]
	public void GetImageFolderPath_HonoursAbsoluteConfiguredRoot()
	{
		// Arrange
		// Kontejner mapuje úložiště na absolutní cestu k volume.
		var listingId = Guid.NewGuid();
		var root = Path.Combine(Path.GetTempPath(), "reality-scraper-images");
		var sut = CreateSut(root);

		// Act
		var folder = sut.GetImageFolderPath(listingId);

		// Assert
		Assert.Equal(Path.Combine(root, listingId.ToString()[..2]), folder);
	}

	[Fact]
	public void GetImageFilePath_IsStableForTheSameListing()
	{
		// Arrange
		// Stahování, čtení i doplňování náhledů musí sáhnout na tentýž soubor.
		var listingId = Guid.NewGuid();
		var sut = CreateSut("files/images");

		// Act & Assert
		Assert.Equal(sut.GetImageFilePath(listingId), sut.GetImageFilePath(listingId));
	}
}

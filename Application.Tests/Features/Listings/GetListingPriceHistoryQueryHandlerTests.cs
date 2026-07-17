using Moq;
using RealityScraper.Application.Features.Listings.GetPriceHistory;
using RealityScraper.Application.Interfaces.Repositories.Realty;
using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Application.Tests.Features.Listings;

public class GetListingPriceHistoryQueryHandlerTests
{
	private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

	private readonly Mock<IListingRepository> listingRepositoryMock = new();

	private GetListingPriceHistoryQueryHandler CreateSut()
	{
		return new GetListingPriceHistoryQueryHandler(listingRepositoryMock.Object);
	}

	private static Listing CreateListing(decimal? price, DateTimeOffset priceFrom)
	{
		return new Listing
		{
			Id = Guid.NewGuid(),
			ExternalId = "ext-1",
			Title = "Prodej domu",
			Location = "Brno",
			Url = "https://example.com/1",
			ImageUrl = string.Empty,
			CreatedAt = Now,
			LastSeenAt = Now,
			Price = price,
			PriceFrom = priceFrom
		};
	}

	[Fact]
	public async Task Handle_ReturnsNotFound_WhenListingDoesNotExist()
	{
		// Arrange
		listingRepositoryMock
			.Setup(x => x.GetWithPriceHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Listing?)null);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingPriceHistoryQuery(Guid.NewGuid()), CancellationToken.None);

		// Assert
		Assert.True(result.IsFailure);
		Assert.Equal("Listing.NotFound", result.Error.Code);
	}

	[Fact]
	public async Task Handle_ReturnsOnlyCurrentPrice_WhenListingHasNoHistory()
	{
		// Arrange
		var listing = CreateListing(5_000_000, Now);
		listingRepositoryMock
			.Setup(x => x.GetWithPriceHistoryAsync(listing.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(listing);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingPriceHistoryQuery(listing.Id), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		var record = Assert.Single(result.Value);
		Assert.Equal(5_000_000, record.Price);
		Assert.Equal(Now, record.RecordedAt);
		Assert.True(record.IsCurrent);
	}

	[Fact]
	public async Task Handle_ReturnsHistoryOrderedByDateWithCurrentPriceLast()
	{
		// Arrange
		var listing = CreateListing(4_500_000, Now);
		listing.PriceHistories =
		[
			new PriceHistory { Price = 5_200_000, RecordedAt = Now.AddDays(-10) },
			new PriceHistory { Price = 5_000_000, RecordedAt = Now.AddDays(-30) }
		];
		listingRepositoryMock
			.Setup(x => x.GetWithPriceHistoryAsync(listing.Id, It.IsAny<CancellationToken>()))
			.ReturnsAsync(listing);
		var sut = CreateSut();

		// Act
		var result = await sut.Handle(new GetListingPriceHistoryQuery(listing.Id), CancellationToken.None);

		// Assert
		Assert.True(result.IsSuccess);
		Assert.Equal(3, result.Value.Count);
		Assert.Equal([5_000_000, 5_200_000, 4_500_000], result.Value.Select(r => r.Price));
		Assert.Equal([Now.AddDays(-30), Now.AddDays(-10), Now], result.Value.Select(r => r.RecordedAt));
		Assert.Equal([false, false, true], result.Value.Select(r => r.IsCurrent));
	}
}

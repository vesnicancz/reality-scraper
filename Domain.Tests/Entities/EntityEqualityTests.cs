using RealityScraper.Domain.Entities.Realty;

namespace RealityScraper.Domain.Tests.Entities;

// Entity porovnává podle Id a konkrétního typu (viz SharedKernel.Entity)
public class EntityEqualityTests
{
	[Fact]
	public void Equals_SameTypeAndSameId_AreEqual()
	{
		var id = Guid.NewGuid();
		var first = new Listing { Id = id, Title = "Chalupa" };
		var second = new Listing { Id = id, Title = "Úplně jiný titulek" };

		Assert.True(first.Equals(second));
		Assert.True(first == second);
		Assert.False(first != second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}

	[Fact]
	public void Equals_SameTypeDifferentId_AreNotEqual()
	{
		var first = new Listing { Id = Guid.NewGuid() };
		var second = new Listing { Id = Guid.NewGuid() };

		Assert.False(first.Equals(second));
		Assert.True(first != second);
	}

	[Fact]
	public void Equals_DifferentTypeWithSameId_AreNotEqual()
	{
		var id = Guid.NewGuid();
		var listing = new Listing { Id = id };
		var priceHistory = new PriceHistory { Id = id };

		Assert.False(listing.Equals(priceHistory));
		Assert.False(priceHistory.Equals(listing));
	}

	[Fact]
	public void Equals_NonEntityObject_IsNotEqual()
	{
		var listing = new Listing { Id = Guid.NewGuid() };

		Assert.False(listing.Equals("nejsem entita"));
		Assert.False(listing.Equals(null));
	}

	[Fact]
	public void EqualityOperator_HandlesNullOnBothSides()
	{
		Listing? nullListing = null;
		var listing = new Listing { Id = Guid.NewGuid() };

		Assert.True(nullListing == null);
		Assert.False(nullListing == listing);
		Assert.False(listing == nullListing);
		Assert.True(listing != nullListing);
	}
}

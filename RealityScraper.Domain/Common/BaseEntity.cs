namespace RealityScraper.Domain.Common;

public abstract class BaseEntity
{
	public Guid Id { get; set; }

	// Metody pro porovnávání entit
	public override bool Equals(object? obj)
	{
		if (obj is BaseEntity other)
		{
			return Id == other.Id;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}
}
namespace RealityScraper.SharedKernel;

public abstract class Entity
{
	public Guid Id { get; set; }

	// Metody pro porovnávání entit
	public override bool Equals(object? obj)
	{
		if (obj is not Entity other)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		if (GetType() != other.GetType())
		{
			return false;
		}

		return Id == other.Id;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode();
	}

	public static bool operator ==(Entity? left, Entity? right)
	{
		return left?.Equals(right) ?? (right is null);
	}

	public static bool operator !=(Entity? left, Entity? right)
	{
		return !(left == right);
	}
}
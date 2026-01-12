using System.Diagnostics.CodeAnalysis;

namespace RealityScraper.SharedKernel;

public class Result<TValue> : Result
{
	private readonly TValue? value;

	public Result(TValue? value, bool isSuccess, Error error)
		: base(isSuccess, error)
	{
		this.value = value;
	}

	[NotNull]
	public TValue Value => IsSuccess
		? value!
		: throw new InvalidOperationException("The value of a failure result can't be accessed.");

#pragma warning disable CA2225 // Operator overloads have named alternates

	public static implicit operator Result<TValue>(TValue? value) =>
		value is not null ? Success(value) : Failure<TValue>(Error.NullValue);

#pragma warning restore CA2225 // Operator overloads have named alternates

#pragma warning disable CA1000 // Do not declare static members on generic types

	public static Result<TValue> ValidationFailure(Error error) =>
		new(default, false, error);

#pragma warning restore CA1000 // Do not declare static members on generic types
}
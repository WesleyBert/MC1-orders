using System.Diagnostics.CodeAnalysis;

namespace Orders.Domain.Common;

/// <summary>Resultado de uma operação que pode falhar por regra de negócio.</summary>
public class Result
{
    protected Result(Error? error) => Error = error;

    public Error? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static Result Success() => new(null);

    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Resultado com valor em caso de sucesso.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Error error) : base(error) { }

    /// <summary>Valor do sucesso. Lança se acessado em uma falha (erro de programação).</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Result em falha ({Error.Code}) não possui valor.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}

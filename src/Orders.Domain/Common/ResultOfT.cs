namespace Orders.Domain.Common;

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

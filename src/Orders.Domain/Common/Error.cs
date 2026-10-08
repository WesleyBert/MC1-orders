namespace Orders.Domain.Common;

public sealed record Error(string Code, string Message, ErrorType Type)
{
    public IReadOnlyDictionary<string, string[]>? Fields { get; init; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> fields) =>
        new("request.validation", "Um ou mais campos são inválidos.", ErrorType.Validation) { Fields = fields };
}

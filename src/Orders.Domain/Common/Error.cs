namespace Orders.Domain.Common;

/// <summary>Erro de negócio esperado. <paramref name="Code"/> é estável e legível por máquina.</summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Mensagens por campo, presentes apenas em erros de validação.</summary>
    public IReadOnlyDictionary<string, string[]>? Fields { get; init; }

    public static Error Validation(IReadOnlyDictionary<string, string[]> fields) =>
        new("request.validation", "Um ou mais campos são inválidos.", ErrorType.Validation) { Fields = fields };
}

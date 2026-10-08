namespace Orders.Domain.Common;

/// <summary>Erro de negócio esperado. <paramref name="Code"/> é estável e legível por máquina.</summary>
public sealed record Error(string Code, string Message, ErrorType Type);

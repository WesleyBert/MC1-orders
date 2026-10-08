namespace Orders.Domain.Common;

/// <summary>Categoria do erro, usada pela API para escolher o status HTTP.</summary>
public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    PreconditionFailed = 4,
}

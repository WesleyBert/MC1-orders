namespace Orders.Domain.Common;

public enum ErrorType
{
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    PreconditionFailed = 4,
}

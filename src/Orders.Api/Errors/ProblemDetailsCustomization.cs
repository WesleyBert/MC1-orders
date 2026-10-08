using Microsoft.AspNetCore.WebUtilities;

namespace Orders.Api.Errors;

public static class ProblemDetailsCustomization
{
    public static void Apply(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var status = problem.Status ?? context.HttpContext.Response.StatusCode;

        problem.Status = status;
        problem.Type = $"https://httpstatuses.io/{status}";
        problem.Title = problem is HttpValidationProblemDetails
            ? "Um ou mais campos são inválidos."
            : TitleFor(status);
        problem.Instance ??= context.HttpContext.Request.Path;
        problem.Detail ??= DefaultDetailFor(status);

        if (!problem.Extensions.ContainsKey("code") && DefaultCodeFor(status) is { } code)
        {
            problem.Extensions["code"] = code;
        }
    }

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Requisição inválida",
        StatusCodes.Status404NotFound => "Não encontrado",
        StatusCodes.Status405MethodNotAllowed => "Método não permitido",
        StatusCodes.Status409Conflict => "Conflito",
        StatusCodes.Status412PreconditionFailed => "Pré-condição falhou",
        StatusCodes.Status413PayloadTooLarge => "Corpo da requisição muito grande",
        StatusCodes.Status415UnsupportedMediaType => "Tipo de mídia não suportado",
        StatusCodes.Status500InternalServerError => "Erro interno",
        _ => ReasonPhrases.GetReasonPhrase(status),
    };

    private static string? DefaultDetailFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "Recurso não encontrado.",
        StatusCodes.Status405MethodNotAllowed => "Método HTTP não permitido para este recurso.",
        StatusCodes.Status413PayloadTooLarge => "O corpo da requisição excede 64 KB.",
        StatusCodes.Status415UnsupportedMediaType => "Envie o corpo como application/json.",
        >= StatusCodes.Status500InternalServerError => "Ocorreu um erro inesperado. Informe o traceId ao suporte.",
        _ => null,
    };

    private static string? DefaultCodeFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "resource.not_found",
        StatusCodes.Status405MethodNotAllowed => "request.method_not_allowed",
        StatusCodes.Status413PayloadTooLarge => "request.too_large",
        StatusCodes.Status415UnsupportedMediaType => "request.unsupported_media_type",
        >= StatusCodes.Status500InternalServerError => "server.unexpected",
        _ => null,
    };
}

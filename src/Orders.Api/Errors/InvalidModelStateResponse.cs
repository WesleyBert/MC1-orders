using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orders.Api.Errors;

public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var bodyParameters = context.ActionDescriptor.Parameters
            .Where(p => p.BindingInfo?.BindingSource == BindingSource.Body)
            .Select(p => p.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var malformedBody = context.ModelState.Any(entry =>
            entry.Value is { Errors.Count: > 0 } &&
            (entry.Key.Length == 0
             || entry.Key.StartsWith('$')
             || bodyParameters.Contains(entry.Key)
             || entry.Value.Errors.Any(e => e.Exception is JsonException)));

        ProblemDetails problem;
        if (malformedBody)
        {
            problem = factory.CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status400BadRequest,
                detail: "O corpo da requisição não é um JSON válido.");
            problem.Extensions["code"] = "request.malformed_json";
        }
        else
        {
            var fields = new ModelStateDictionary();
            foreach (var (key, entry) in context.ModelState)
            {
                if (entry.Errors.Count > 0)
                {
                    fields.AddModelError(ResultExtensions.ToJsonName(key), "O valor informado é inválido.");
                }
            }

            problem = factory.CreateValidationProblemDetails(context.HttpContext, fields, StatusCodes.Status400BadRequest);
            problem.Extensions["code"] = "request.validation";
        }

        return new BadRequestObjectResult(problem) { ContentTypes = { ResultExtensions.ProblemJson } };
    }
}

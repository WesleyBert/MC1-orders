using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orders.Domain.Common;

namespace Orders.Api.Errors;

public static class ResultExtensions
{
    public const string ProblemJson = "application/problem+json";

    public static ObjectResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.PreconditionFailed => StatusCodes.Status412PreconditionFailed,
            _ => StatusCodes.Status500InternalServerError,
        };

        ProblemDetails problem;
        if (error.Fields is { } fields)
        {
            var modelState = new ModelStateDictionary();
            foreach (var (field, messages) in fields)
            {
                foreach (var message in messages)
                {
                    modelState.AddModelError(ToJsonName(field), message);
                }
            }

            problem = controller.ProblemDetailsFactory.CreateValidationProblemDetails(
                controller.HttpContext, modelState, status);
        }
        else
        {
            problem = controller.ProblemDetailsFactory.CreateProblemDetails(
                controller.HttpContext, status, detail: error.Message);
        }

        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { ProblemJson } };
    }

    public static string ToJsonName(string field) =>
        field.Contains('-', StringComparison.Ordinal) ? field : JsonNamingPolicy.CamelCase.ConvertName(field);
}

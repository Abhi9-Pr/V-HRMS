using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vespera.Domain.Common;

namespace Vespera.Api.Http;

/// <summary>Maps a handler's <see cref="Result"/>/<see cref="Result{T}"/> to the matching HTTP
/// status + RFC 7807 ProblemDetails body on failure — every controller action goes through this,
/// so the ErrorType→status mapping lives in exactly one place.</summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult(this Result result, ControllerBase controller) =>
        result.IsSuccess ? controller.NoContent() : ToProblem(controller, result.Error);

    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, IActionResult>? onSuccess = null) =>
        result.IsSuccess
            ? onSuccess is not null ? onSuccess(result.Value) : controller.Ok(result.Value)
            : ToProblem(controller, result.Error);

    private static ObjectResult ToProblem(ControllerBase controller, Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(detail: error.Message, statusCode: statusCode, title: error.Code);
    }
}

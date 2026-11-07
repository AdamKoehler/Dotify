using Microsoft.AspNetCore.Mvc;

namespace Dotify.Server.Common;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Error error)
    {
        var status = StatusFor(error.Kind);

        ProblemDetails problem = error.Fields is { Count: > 0 }
            ? new HttpValidationProblemDetails(
                error.Fields.ToDictionary(f => f.Key, f => f.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = TitleFor(error.Kind);
        problem.Detail = error.Detail;
        problem.Extensions["code"] = error.Code;

        return TypedResults.Problem(problem);
    }

    public static IResult ToHttpResult<T>(
        this Result<T> result,
        int successStatusCode = StatusCodes.Status200OK) =>
        result.IsSuccess
            ? TypedResults.Json(result.Value, statusCode: successStatusCode)
            : result.Error!.ToHttpResult();

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Locked => StatusCodes.Status423Locked,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => "Validation failed",
        ErrorKind.Unauthorized => "Unauthorized",
        ErrorKind.Forbidden => "Forbidden",
        ErrorKind.NotFound => "Not found",
        ErrorKind.Conflict => "Conflict",
        ErrorKind.Locked => "Locked",
        _ => "Unexpected error"
    };
}

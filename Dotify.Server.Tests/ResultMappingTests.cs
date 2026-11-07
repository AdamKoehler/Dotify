using Dotify.Server.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Dotify.Server.Tests;

public class ResultMappingTests
{
    [Theory]
    [InlineData(ErrorKind.Validation, 400)]
    [InlineData(ErrorKind.Unauthorized, 401)]
    [InlineData(ErrorKind.Forbidden, 403)]
    [InlineData(ErrorKind.NotFound, 404)]
    [InlineData(ErrorKind.Conflict, 409)]
    [InlineData(ErrorKind.Locked, 423)]
    public void Error_kind_maps_to_status_code(ErrorKind kind, int expectedStatus)
    {
        var error = new Error(kind, "some_code", "Something went wrong.");

        var problem = Assert.IsType<ProblemHttpResult>(error.ToHttpResult());

        Assert.Equal(expectedStatus, problem.StatusCode);
    }

    [Fact]
    public void Error_code_is_exposed_as_a_problem_details_extension()
    {
        var error = new Error(ErrorKind.Unauthorized, "invalid_credentials", "Bad credentials.");

        var problem = Assert.IsType<ProblemHttpResult>(error.ToHttpResult());

        Assert.Equal("invalid_credentials", Assert.IsType<string>(problem.ProblemDetails.Extensions["code"]));
        Assert.Equal("Bad credentials.", problem.ProblemDetails.Detail);
    }

    [Fact]
    public void Field_errors_produce_a_validation_problem_details()
    {
        var fields = new Dictionary<string, string[]> { ["Password"] = ["Too short."] };
        var error = new Error(ErrorKind.Validation, "invalid_input", "Validation failed.", fields);

        var problem = Assert.IsType<ProblemHttpResult>(error.ToHttpResult());
        var validation = Assert.IsType<HttpValidationProblemDetails>(problem.ProblemDetails);

        Assert.Equal(400, problem.StatusCode);
        Assert.Equal(["Too short."], validation.Errors["Password"]);
    }

    [Fact]
    public void Successful_result_with_a_value_returns_the_value_with_200()
    {
        var result = Result<string>.Ok("hello");

        var ok = Assert.IsType<JsonHttpResult<string>>(result.ToHttpResult());

        Assert.Equal(200, ok.StatusCode);
        Assert.Equal("hello", ok.Value);
    }

    [Fact]
    public void Failed_result_maps_through_its_error()
    {
        var result = Result<string>.Fail(new Error(ErrorKind.Conflict, "username_taken", "Taken."));

        var problem = Assert.IsType<ProblemHttpResult>(result.ToHttpResult());

        Assert.Equal(409, problem.StatusCode);
        Assert.Equal("username_taken", problem.ProblemDetails.Extensions["code"]);
    }
}

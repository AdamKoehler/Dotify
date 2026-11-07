namespace Dotify.Server.Common;

public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Locked
}

public sealed record Error(
    ErrorKind Kind,
    string Code,
    string Detail,
    IReadOnlyDictionary<string, string[]>? Fields = null);

public readonly record struct Result<T>
{
    private Result(T? value, Error? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public Error? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);
    public static Result<T> Fail(Error error) => new(default, error);
}

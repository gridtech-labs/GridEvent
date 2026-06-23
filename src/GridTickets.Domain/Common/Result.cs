namespace GridTickets.Domain.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public IReadOnlyList<string> Errors { get; }

    protected Result(bool isSuccess, string? error, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error, new[] { error });
    public static Result Failure(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new(false, list.FirstOrDefault(), list);
    }
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string? error, IEnumerable<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error, new[] { error });
    public new static Result<T> Failure(IEnumerable<string> errors)
    {
        var list = errors.ToList();
        return new(false, default, list.FirstOrDefault(), list);
    }
}

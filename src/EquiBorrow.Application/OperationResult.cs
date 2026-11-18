namespace EquiBorrow.Application;

public sealed record OperationResult(OperationOutcome Outcome, string Message)
{
    public bool Success => Outcome == OperationOutcome.Success;

    public static OperationResult Succeeded(string message) => new(OperationOutcome.Success, message);
    public static OperationResult Rejected(string message) => new(OperationOutcome.Rejected, message);
    public static OperationResult Stale(string message) => new(OperationOutcome.Stale, message);
    public static OperationResult Failed(string message) => new(OperationOutcome.Failed, message);
    public static OperationResult UnknownOutcome(string message) => new(OperationOutcome.UnknownOutcome, message);
}

public sealed record OperationResult<T>(OperationOutcome Outcome, string Message, T? Value)
{
    public bool Success => Outcome == OperationOutcome.Success;

    public static OperationResult<T> Succeeded(T value, string message) => new(OperationOutcome.Success, message, value);
    public static OperationResult<T> Rejected(string message) => new(OperationOutcome.Rejected, message, default);
    public static OperationResult<T> Stale(string message) => new(OperationOutcome.Stale, message, default);
    public static OperationResult<T> Failed(string message) => new(OperationOutcome.Failed, message, default);
    public static OperationResult<T> UnknownOutcome(string message) => new(OperationOutcome.UnknownOutcome, message, default);
}

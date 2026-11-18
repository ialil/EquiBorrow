namespace EquiBorrow.Application;

public enum PersistenceConflictKind
{
    Duplicate,
    Concurrency
}

public sealed class PersistenceConflictException : Exception
{
    public PersistenceConflictKind Kind { get; }
    public string? ConstraintName { get; }

    public PersistenceConflictException(
        PersistenceConflictKind kind,
        string? constraintName = null,
        string? message = null,
        Exception? innerException = null)
        : base(message ?? (kind == PersistenceConflictKind.Duplicate
            ? "A record with the same unique value already exists."
            : "The record was changed by another operation."), innerException)
    {
        Kind = kind;
        ConstraintName = constraintName;
    }
}

public sealed class PersistenceFailureException : Exception
{
    public PersistenceFailureException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class CommitOutcomeUnknownException : Exception
{
    public CommitOutcomeUnknownException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

namespace UserManager.Application.Common;

public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(string? constraintName, Exception innerException)
        : base($"Unique constraint '{constraintName}' violated.", innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}

namespace AuditService.Infrastructure.Messagings;

public sealed class InvalidAuditEventException : Exception
{
    public InvalidAuditEventException(string reason) : base(reason)
    {
    }
}

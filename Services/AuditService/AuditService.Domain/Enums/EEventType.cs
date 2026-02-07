namespace AuditService.Domain.Enums
{
    public enum EEventType
    {
        UserCreated,
        UserUpdated,
        UserDeleted,
        AdminLoggedIn,
        AdminCreated,
        ApiKeyCreated,
        ApiKeyRevoked,
        ApiKeyValidated,
        AuditLogQueried
    }
}

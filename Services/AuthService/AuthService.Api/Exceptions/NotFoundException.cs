namespace AuthService.Api.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string resourceName, object? resourceId = null) : base($"Resource '{resourceName}' not found{(resourceId != null ? $" with ID '{resourceId}'" : "")}")
        {
            ResourceName = resourceName;
            ResourceId = resourceId;
        }

        public string ResourceName { get; }
        public object? ResourceId { get; }
    }
}

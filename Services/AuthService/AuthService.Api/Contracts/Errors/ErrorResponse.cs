using AuthService.Domain.Common;

namespace AuthService.Api.Contracts.Errors
{
    public class ErrorResponse
    {
        public ErrorResponse(string title, int status, IReadOnlyCollection<ValidationError> errors, string traceId, DateTime timestamp, string? detail)
        {
            Title = title;
            Status = status;
            Errors = errors;
            TraceId = traceId;
            Timestamp = timestamp;
            Detail = detail;
        }

        public string Title { get; }
        public int Status { get; set; }
        public IReadOnlyCollection<ValidationError> Errors { get; }
        public string TraceId { get; }
        public DateTime Timestamp { get; }
        public string? Detail { get; }
    }
}
